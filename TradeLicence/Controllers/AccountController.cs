using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.RegularExpressions;
using TradeLicence.Data;
using TradeLicence.Models;
using TradeLicence.Services;

namespace TradeLicence.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly CaptchaService _captchaService;
        private readonly IPanVerificationService _panService;
        private readonly OtpService _otpService;
        private readonly EmailOtpService _emailOtpService;
        private readonly PasswordHasher<ApplicationUser> _passwordHasher = new();
        private readonly PasswordHasher<Officer> _officerPasswordHasher = new();

        private const int MaxFailedAttempts = 5;
        private const string PanPattern = @"^[A-Z]{3}[ABCFGHLJPT][A-Z][0-9]{4}[A-Z]$";

        public AccountController(ApplicationDbContext context, CaptchaService captchaService,
                                 IPanVerificationService panService, OtpService otpService,
                                 EmailOtpService emailOtpService)
        {
            _context = context;
            _captchaService = captchaService;
            _panService = panService;
            _otpService = otpService;
            _emailOtpService = emailOtpService;
        }

        private static string PanKey(string? pan, string? name, DateTime dob)
            => $"{(pan ?? "").Trim().ToUpperInvariant()}|" +
               $"{string.Join(' ', (name ?? "").Trim().ToUpperInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries))}|" +
               $"{dob:yyyyMMdd}";

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewBag.RegisterModel = new RegisterViewModel();
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            // 1. CAPTCHA verification
            var expectedCode = HttpContext.Session.GetString("CaptchaCode");
            HttpContext.Session.Remove("CaptchaCode");

            bool captchaValid = !string.IsNullOrEmpty(expectedCode) &&
                string.Equals(model.CaptchaInput?.Trim(), expectedCode, StringComparison.OrdinalIgnoreCase);

            var typedPassword = model.Password;
            model.CaptchaInput = string.Empty;
            model.Password = string.Empty;
            ModelState.Remove(nameof(model.CaptchaInput));
            ModelState.Remove(nameof(model.Password));

            if (!captchaValid)
            {
                ModelState.AddModelError(nameof(model.CaptchaInput), "The code entered does not match the image. Please try again.");
                return View(model);
            }

            if (!ModelState.IsValid) return View(model);

            // 2. User validation
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == model.Username);

            const string genericError = "Invalid username or password.";

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, genericError);
                return View(model);
            }

            if (user.IsLocked)
            {
                ModelState.AddModelError(string.Empty, "This account is locked due to repeated failed login attempts. Please contact support.");
                return View(model);
            }

            // 3. Email OTP verification
            var otp = (model.Otp ?? "").Trim();
            if (!Regex.IsMatch(otp, @"^\d{6}$"))
            {
                ModelState.AddModelError(nameof(model.Otp), "Please enter the 6-digit OTP sent to your registered email.");
                return View(model);
            }

            var (otpOk, otpMsg) = _emailOtpService.Verify(HttpContext.Session, user.Email ?? "", otp);
            if (!otpOk)
            {
                ModelState.AddModelError(nameof(model.Otp), otpMsg);
                return View(model);
            }

            // 4. Password verification
            var verifyResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, typedPassword);
            if (verifyResult == PasswordVerificationResult.Failed)
            {
                user.FailedLoginAttempts++;
                if (user.FailedLoginAttempts >= MaxFailedAttempts)
                {
                    user.IsLocked = true;
                }
                await _context.SaveChangesAsync();

                ModelState.AddModelError(string.Empty, genericError);
                return View(model);
            }

            // 5. Sign in
            user.FailedLoginAttempts = 0;
            user.LastLoginDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, user.Username),
                new(ClaimTypes.NameIdentifier, user.UserId.ToString())
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties { IsPersistent = false, ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30) });

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                return Redirect(model.ReturnUrl);

            return RedirectToAction("Index", "Dashboard");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendLoginEmailOtp([FromBody] SendLoginOtpRequest req)
        {
            var username = (req?.Username ?? "").Trim();
            if (string.IsNullOrWhiteSpace(username))
                return Json(new { success = false, message = "Please enter your User Name first." });

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null || string.IsNullOrWhiteSpace(user.Email))
                return Json(new { success = false, message = "No registered email found for this User Name." });

            var (ok, message) = await _emailOtpService.SendAsync(HttpContext.Session, user.Email);
            return Json(new { success = ok, message });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return await RedisplayLoginWithRegisterErrors(model);
            }

            var usernameTaken = await _context.Users.AnyAsync(u => u.Username == model.Username);
            if (usernameTaken)
            {
                ModelState.AddModelError(nameof(model.Username), "This username is already taken.");
            }

            var emailTaken = await _context.Users.AnyAsync(u => u.Email == model.Email);
            if (emailTaken)
            {
                ModelState.AddModelError(nameof(model.Email), "This email is already registered.");
            }

            var panUpper = model.PANNumber.ToUpperInvariant();
            if (await _context.Users.AnyAsync(u => u.PANNumber == panUpper))
            {
                ModelState.AddModelError(nameof(model.PANNumber), "This PAN is already registered.");
            }

       

            if (!ModelState.IsValid)
            {
                return await RedisplayLoginWithRegisterErrors(model);
            }

            var newUser = new ApplicationUser
            {
                Username = model.Username,
                Email = model.Email,
                FullName = model.FullName,
                DateOfBirth = model.DateOfBirth,
                PANNumber = model.PANNumber.ToUpperInvariant(),
                MobileNumber = model.MobileNumber,
                Address = model.Address,
                CreatedDate = DateTime.UtcNow,
                IsLocked = false,
                FailedLoginAttempts = 0
            };
            newUser.PasswordHash = _passwordHasher.HashPassword(newUser, model.Password);

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            HttpContext.Session.Remove("Pan_Verified");
            _otpService.ClearVerified(HttpContext.Session);
            _emailOtpService.ClearVerified(HttpContext.Session);

            TempData["RegisterSuccess"] = $"Welcome, {newUser.Username}! Your account has been created successfully. Please login to continue.";

            return RedirectToAction("Login");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyPan([FromBody] VerifyPanRequest req)
        {
            var pan = (req.PanNumber ?? "").Trim().ToUpperInvariant();

            if (!Regex.IsMatch(pan, PanPattern))
                return Json(new { success = false, message = "Enter a valid PAN number (e.g. ABCDE1234F)." });
            if (string.IsNullOrWhiteSpace(req.FullName))
                return Json(new { success = false, message = "Enter your name as per PAN." });
            if (req.DateOfBirth.Year < 1900 || req.DateOfBirth.Date >= DateTime.Today)
                return Json(new { success = false, message = "Enter a valid date of birth." });
            if (await _context.Users.AnyAsync(u => u.PANNumber == pan))
                return Json(new { success = false, message = "This PAN is already registered." });

            var result = await _panService.VerifyAsync(pan, req.FullName.Trim(), req.DateOfBirth);
            if (!result.Verified)
            {
                HttpContext.Session.Remove("Pan_Verified");
                return Json(new { success = false, message = result.Message });
            }

            HttpContext.Session.SetString("Pan_Verified", PanKey(pan, req.FullName, req.DateOfBirth));
            return Json(new { success = true, message = result.Message });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendOtp([FromBody] SendOtpRequest req)
        {
            var mobile = (req.MobileNumber ?? "").Trim();
            if (!Regex.IsMatch(mobile, @"^[6-9]\d{9}$"))
                return Json(new { success = false, message = "Enter a valid 10-digit mobile number starting with 6, 7, 8 or 9." });

            if (await _context.Users.AnyAsync(u => u.MobileNumber == mobile))
                return Json(new { success = false, message = "This mobile number is already registered." });

            var (ok, message) = await _otpService.SendAsync(HttpContext.Session, mobile);
            return Json(new { success = ok, message });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult VerifyOtp([FromBody] VerifyOtpRequest req)
        {
            var mobile = (req.MobileNumber ?? "").Trim();
            var otp = (req.Otp ?? "").Trim();
            if (!Regex.IsMatch(otp, @"^\d{6}$"))
                return Json(new { success = false, message = "Enter the 6-digit OTP." });

            var (ok, message) = _otpService.Verify(HttpContext.Session, mobile, otp);
            return Json(new { success = ok, message });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendEmailOtp([FromBody] SendEmailOtpRequest req)
        {
            var email = (req.Email ?? "").Trim();
            if (email.Length > 150 || !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email))
                return Json(new { success = false, message = "Enter a valid email address." });

            if (await _context.Users.AnyAsync(u => u.Email == email))
                return Json(new { success = false, message = "This email is already registered." });

            var (ok, message) = await _emailOtpService.SendAsync(HttpContext.Session, email);
            return Json(new { success = ok, message });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult VerifyEmailOtp([FromBody] VerifyEmailOtpRequest req)
        {
            var email = (req.Email ?? "").Trim();
            var otp = (req.Otp ?? "").Trim();
            if (!Regex.IsMatch(otp, @"^\d{6}$"))
                return Json(new { success = false, message = "Enter the 6-digit OTP." });

            var (ok, message) = _emailOtpService.Verify(HttpContext.Session, email, otp);
            return Json(new { success = ok, message });
        }
        //  FORGOT PASSWORD
        //  Step 1 : user enters User Name + Mobile + Email + Captcha.
        //           We check all three match ONE registered account, then send an OTP
        //           to the registered mobile AND the registered email.
        //  Step 2 : user enters both OTPs + new password + confirm password.
        //           We verify the OTPs and save the new (hashed) password.
        // =====================================================================
        private const string FpUserKey = "Fp_User";
        private const string FpMobileKey = "Fp_Mobile";
        private const string FpEmailKey = "Fp_Email";

        private void ClearForgotSession()
        {
            HttpContext.Session.Remove(FpUserKey);
            HttpContext.Session.Remove(FpMobileKey);
            HttpContext.Session.Remove(FpEmailKey);
            _otpService.ClearVerified(HttpContext.Session);
            _emailOtpService.ClearVerified(HttpContext.Session);
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            ClearForgotSession();
            ViewBag.FpStage = 1;
            return View(new ForgotPasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPasswordSend(ForgotPasswordViewModel model)
        {
            ClearForgotSession();            // every new attempt starts fresh
            ViewBag.FpStage = 1;

            // ---- 1. CAPTCHA first (single use) ----
            var expectedCode = HttpContext.Session.GetString("CaptchaCode");
            HttpContext.Session.Remove("CaptchaCode");
            bool captchaValid = !string.IsNullOrEmpty(expectedCode) &&
                string.Equals(model.CaptchaInput?.Trim(), expectedCode, StringComparison.OrdinalIgnoreCase);
            model.CaptchaInput = string.Empty;
            ModelState.Remove(nameof(model.CaptchaInput));

            var username = (model.Username ?? "").Trim();
            var mobile = (model.MobileNumber ?? "").Trim();
            var email = (model.Email ?? "").Trim();

            // ---- 2. Basic checks on what was typed ----
            if (username.Length == 0)
                ModelState.AddModelError(nameof(model.Username), "User name is required.");
            if (!Regex.IsMatch(mobile, @"^[6-9]\d{9}$"))
                ModelState.AddModelError(nameof(model.MobileNumber), "Enter the 10-digit registered mobile number.");
            if (email.Length == 0 || email.Length > 150 ||
                !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email))
                ModelState.AddModelError(nameof(model.Email), "Enter the registered email address.");
            if (!captchaValid)
                ModelState.AddModelError(nameof(model.CaptchaInput), "The code entered does not match the image. Please try again.");

            if (!ModelState.IsValid) return View("ForgotPassword", model);

            // ---- 3. All three details must belong to the SAME account ----
            var user = await _context.Users.FirstOrDefaultAsync(u =>
                u.Username == username && u.MobileNumber == mobile && u.Email == email);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "The details you entered do not match any registered account.");
                return View("ForgotPassword", model);
            }

            // ---- 4. Send OTP to the registered mobile and email ----
            var sms = await _otpService.SendAsync(HttpContext.Session, mobile);
            if (!sms.ok)
            {
                ModelState.AddModelError(string.Empty, sms.message);
                return View("ForgotPassword", model);
            }

            var mail = await _emailOtpService.SendAsync(HttpContext.Session, email);
            if (!mail.ok)
            {
                ModelState.AddModelError(string.Empty, mail.message);
                return View("ForgotPassword", model);
            }

            HttpContext.Session.SetString(FpUserKey, user.Username);
            HttpContext.Session.SetString(FpMobileKey, mobile);
            HttpContext.Session.SetString(FpEmailKey, email);

            model.Username = user.Username;
            model.MobileNumber = mobile;
            model.Email = email;
            ViewBag.FpStage = 2;
            ViewBag.FpInfo = sms.message + " " + mail.message;
            return View("ForgotPassword", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPasswordReset(ForgotPasswordViewModel model)
        {
            var username = HttpContext.Session.GetString(FpUserKey);
            var mobile = HttpContext.Session.GetString(FpMobileKey);
            var email = HttpContext.Session.GetString(FpEmailKey);

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(mobile) || string.IsNullOrEmpty(email))
            {
                TempData["FpExpired"] = "Your session expired. Please start again.";
                return RedirectToAction(nameof(ForgotPassword));
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null)
            {
                ClearForgotSession();
                return RedirectToAction(nameof(ForgotPassword));
            }

            // Show the same (read-only) details again if we have to redisplay the page
            model.Username = user.Username;
            model.MobileNumber = mobile;
            model.Email = email;
            ViewBag.FpStage = 2;

            var mobileOtp = (model.MobileOtp ?? "").Trim();
            var emailOtp = (model.EmailOtp ?? "").Trim();
            var newPassword = model.NewPassword ?? "";
            var confirmPassword = model.ConfirmPassword ?? "";

            // never send typed passwords / OTPs back to the browser
            model.MobileOtp = string.Empty; model.EmailOtp = string.Empty;
            model.NewPassword = string.Empty; model.ConfirmPassword = string.Empty;
            ModelState.Remove(nameof(model.MobileOtp)); ModelState.Remove(nameof(model.EmailOtp));
            ModelState.Remove(nameof(model.NewPassword)); ModelState.Remove(nameof(model.ConfirmPassword));

            bool mobileDone = _otpService.IsVerified(HttpContext.Session, mobile);
            bool emailDone = _emailOtpService.IsVerified(HttpContext.Session, email);

            if (!mobileDone && !Regex.IsMatch(mobileOtp, @"^\d{6}$"))
                ModelState.AddModelError(nameof(model.MobileOtp), "Enter the 6-digit mobile OTP.");
            if (!emailDone && !Regex.IsMatch(emailOtp, @"^\d{6}$"))
                ModelState.AddModelError(nameof(model.EmailOtp), "Enter the 6-digit email OTP.");
            if (newPassword.Length < 6 || newPassword.Length > 15)
                ModelState.AddModelError(nameof(model.NewPassword), "Password must be 6-15 characters.");
            if (newPassword != confirmPassword)
                ModelState.AddModelError(nameof(model.ConfirmPassword), "Passwords do not match.");

            if (!ModelState.IsValid) return View("ForgotPassword", model);

            // ---- check the OTPs (a code already verified earlier is not asked again) ----
            if (!mobileDone)
            {
                var r = _otpService.Verify(HttpContext.Session, mobile, mobileOtp);
                if (!r.ok) ModelState.AddModelError(nameof(model.MobileOtp), r.message);
            }
            if (!emailDone)
            {
                var r = _emailOtpService.Verify(HttpContext.Session, email, emailOtp);
                if (!r.ok) ModelState.AddModelError(nameof(model.EmailOtp), r.message);
            }
            if (!ModelState.IsValid) return View("ForgotPassword", model);

            // ---- save the new password (hashed, never plain text) and unlock the account ----
            user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);
            user.FailedLoginAttempts = 0;
            user.IsLocked = false;
            await _context.SaveChangesAsync();

            ClearForgotSession();
            TempData["ResetSuccess"] = "Your password has been updated. Please login with your new password.";
            return RedirectToAction(nameof(Login));
        }

        private Task<IActionResult> RedisplayLoginWithRegisterErrors(RegisterViewModel model)
        {
            model.Password = string.Empty;
            model.ConfirmPassword = string.Empty;
            ModelState.Remove(nameof(model.Password));
            ModelState.Remove(nameof(model.ConfirmPassword));

            ViewBag.RegisterModel = model;
            ViewBag.PanVerified = HttpContext.Session.GetString("Pan_Verified") == PanKey(model.PANNumber, model.FullName, model.DateOfBirth);
            ViewBag.MobileVerified = _otpService.IsVerified(HttpContext.Session, model.MobileNumber);
            ViewBag.EmailVerified = _emailOtpService.IsVerified(HttpContext.Session, model.Email);
            ViewBag.ShowRegisterModal = true;
            return Task.FromResult<IActionResult>(View("Login", new LoginViewModel()));
        }

        [HttpGet]
        public IActionResult CaptchaImage()
        {
            var code = _captchaService.GenerateCode();
            HttpContext.Session.SetString("CaptchaCode", code);
            var svg = _captchaService.RenderSvg(code);

            Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
            return Content(svg, "image/svg+xml");
        }

        // ---------------- Officer Login ----------------

        [HttpGet]
        public IActionResult OfficerLogin(string? returnUrl = null)
        {
            return View(new OfficerLoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OfficerLogin(OfficerLoginViewModel model)
        {
            var expectedCode = HttpContext.Session.GetString("CaptchaCode");
            HttpContext.Session.Remove("CaptchaCode");

            bool captchaValid = !string.IsNullOrEmpty(expectedCode) &&
                string.Equals(model.CaptchaInput?.Trim(), expectedCode, StringComparison.OrdinalIgnoreCase);

            var typedPassword = model.Password;
            model.CaptchaInput = string.Empty;
            model.Password = string.Empty;
            ModelState.Remove(nameof(model.CaptchaInput));
            ModelState.Remove(nameof(model.Password));

            if (!captchaValid)
            {
                ModelState.AddModelError(nameof(model.CaptchaInput), "The code entered does not match the image. Please try again.");
                return View(model);
            }

            if (!ModelState.IsValid) return View(model);

            var officer = await _context.Officers.FirstOrDefaultAsync(o => o.Username == model.Username);

            const string genericError = "Invalid username or password.";

            if (officer == null)
            {
                ModelState.AddModelError(string.Empty, genericError);
                return View(model);
            }

            if (officer.IsLocked)
            {
                ModelState.AddModelError(string.Empty, "This account is locked due to repeated failed login attempts. Please contact the system administrator.");
                return View(model);
            }

            var verifyResult = _officerPasswordHasher.VerifyHashedPassword(officer, officer.PasswordHash, typedPassword);
            if (verifyResult == PasswordVerificationResult.Failed)
            {
                officer.FailedLoginAttempts++;
                if (officer.FailedLoginAttempts >= MaxFailedAttempts)
                {
                    officer.IsLocked = true;
                }
                await _context.SaveChangesAsync();

                ModelState.AddModelError(string.Empty, genericError);
                return View(model);
            }

            officer.FailedLoginAttempts = 0;
            officer.LastLoginDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, officer.Username),
                new(ClaimTypes.NameIdentifier, officer.OfficerId.ToString()),
                new(ClaimTypes.Role, "Officer"),
                new("Designation", officer.Designation ?? string.Empty)
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties { IsPersistent = false, ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30) });

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                return Redirect(model.ReturnUrl);

            return RedirectToAction("Index", "Officer");
        }

        // ---------------- Profile Popover ----------------

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> UserProfile()
        {
            if (User.IsInRole("Officer"))
                return StatusCode(StatusCodes.Status403Forbidden);

            if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
                return Unauthorized();

            var profile = await _context.Users
                .AsNoTracking()
                .Where(u => u.UserId == userId)
                .Select(u => new
                {
                    u.UserId,
                    u.Username,
                    u.FullName,
                    u.Email,
                    u.MobileNumber,
                    u.PANNumber,
                    u.Address,
                    DateOfBirth = u.DateOfBirth != null ? u.DateOfBirth.Value.ToString("yyyy-MM-dd") : null,
                    u.IsLocked,
                    u.FailedLoginAttempts,
                    LastLoginDate = AsUtc(u.LastLoginDate),
                    CreatedDate = AsUtc(u.CreatedDate)
                })
                .FirstOrDefaultAsync();

            if (profile == null) return NotFound();

            return Json(profile);
        }

        [Authorize(Roles = "Officer")]
        [HttpGet]
        public async Task<IActionResult> OfficerProfile()
        {
            if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var officerId))
                return Unauthorized();

            var profile = await _context.Officers
                .AsNoTracking()
                .Where(o => o.OfficerId == officerId)
                .Select(o => new
                {
                    o.OfficerId,
                    o.Username,
                    o.FullName,
                    o.Department,
                    o.Designation,
                    o.Email,
                    o.IsLocked,
                    o.FailedLoginAttempts,
                    LastLoginDate = AsUtc(o.LastLoginDate),
                    CreatedDate = AsUtc(o.CreatedDate),
                    o.CreatedBy
                })
                .FirstOrDefaultAsync();

            if (profile == null) return NotFound();

            return Json(profile);
        }

        private static DateTime? AsUtc(DateTime? value) =>
            value.HasValue ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : null;

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }

    public class SendLoginOtpRequest
    {
        public string Username { get; set; } = string.Empty;
    }
}