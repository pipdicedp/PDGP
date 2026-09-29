using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TradeLicence.Data;
using TradeLicence.Models;
using TradeLicence.Services;

namespace TradeLicence.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly CaptchaService _captchaService;
        private readonly PasswordHasher<ApplicationUser> _passwordHasher = new();
        private readonly PasswordHasher<Officer> _officerPasswordHasher = new();

        private const int MaxFailedAttempts = 5;

        public AccountController(ApplicationDbContext context, CaptchaService captchaService)
        {
            _context = context;
            _captchaService = captchaService;
        }

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
            // ---- 1. CAPTCHA check FIRST, before touching the database at all ----
            var expectedCode = HttpContext.Session.GetString("CaptchaCode");
            HttpContext.Session.Remove("CaptchaCode"); // single-use, whether it matches or not

            bool captchaValid = !string.IsNullOrEmpty(expectedCode) &&
                string.Equals(model.CaptchaInput?.Trim(), expectedCode, StringComparison.OrdinalIgnoreCase);

            // A fresh CAPTCHA image is generated every time this view is redisplayed
            // (the <img> tag always calls CaptchaImage() again), so whatever the user
            // typed for the OLD image is now meaningless. Clear it from both the model
            // AND ModelState — asp-for reads ModelState's attempted value first, so
            // clearing only the model property is not enough; the old text would still
            // reappear in the textbox otherwise.
            var typedPassword = model.Password; // keep the real value for verification below
            model.CaptchaInput = string.Empty;
            model.Password = string.Empty;      // cleared for redisplay only — browsers
                                                // silently refill a wrong password otherwise
            ModelState.Remove(nameof(model.CaptchaInput));
            ModelState.Remove(nameof(model.Password));

            if (!captchaValid)
            {
                ModelState.AddModelError(nameof(model.CaptchaInput), "The code entered does not match the image. Please try again.");
                return View(model);
            }

            if (!ModelState.IsValid) return View(model);

            // ---- 2. Look up the user ----
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

            // ---- 3. Success: reset failed-attempt counter, sign in ----
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

        /// <summary>
        /// Handles the "New User? Register Here" modal on the Login page.
        /// On validation failure (server-side or duplicate username/email),
        /// re-renders the Login view with the register modal re-opened and
        /// the entered values preserved, instead of losing the form.
        /// </summary>
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

            TempData["RegisterSuccess"] = $"Welcome, {newUser.Username}! Your account has been created successfully. Please login to continue.";

            return RedirectToAction("Login");
        }

        private Task<IActionResult> RedisplayLoginWithRegisterErrors(RegisterViewModel model)
        {
            model.Password = string.Empty;
            model.ConfirmPassword = string.Empty;
            ModelState.Remove(nameof(model.Password));
            ModelState.Remove(nameof(model.ConfirmPassword));

            ViewBag.RegisterModel = model;
            ViewBag.ShowRegisterModal = true;
            return Task.FromResult<IActionResult>(View("Login", new LoginViewModel()));
        }

        /// <summary>
        /// Generates a fresh CAPTCHA code each time it's requested, stores the
        /// code server-side in Session, and returns an SVG image of it. The
        /// login page's "refresh" icon just reloads this <img> with a cache-buster.
        /// Shared by both the citizen Login page and the Officer Login page.
        /// </summary>
        [HttpGet]
        public IActionResult CaptchaImage()
        {
            var code = _captchaService.GenerateCode();
            HttpContext.Session.SetString("CaptchaCode", code);
            var svg = _captchaService.RenderSvg(code);

            Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
            return Content(svg, "image/svg+xml");
        }

        // ---------------- Officer (Official) Login ----------------

        [HttpGet]
        public IActionResult OfficerLogin(string? returnUrl = null)
        {
            return View(new OfficerLoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OfficerLogin(OfficerLoginViewModel model)
        {
            // ---- 1. CAPTCHA check FIRST, before touching the database at all ----
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

            // ---- 2. Look up the officer ----
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

            // ---- 3. Success: reset failed-attempt counter, sign in ----
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

            // Every officer lands here — Index itself routes on by Department
            // (TradeLicence's own logic, or the shared queue for every other service).
            return RedirectToAction("Index", "Officer");
        }

        // ---------------- Profile popover (user-circle icon in the nav bar) ----------------
        //
        // Both endpoints are read-only JSON, called by wwwroot/js/user-profile.js
        // when the user-circle icon is clicked. They return ONLY the logged-in
        // person's own row (id comes from the auth cookie, never from the
        // request), and PasswordHash is deliberately NOT selected.

        /// <summary>Citizen profile — dbo.Users.</summary>
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> UserProfile()
        {
            // Citizens and officers share the same cookie scheme and both put
            // their own table's primary key in ClaimTypes.NameIdentifier. Without
            // this check an officer with OfficerId = 1 would be shown the details
            // of the citizen with UserId = 1.
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
                    u.DateOfBirth,
                    u.IsLocked,
                    u.FailedLoginAttempts,
                    u.LastLoginDate,
                    u.CreatedDate
                })
                .FirstOrDefaultAsync();

            if (profile == null) return NotFound();

            return Json(new
            {
                profile.UserId,
                profile.Username,
                profile.FullName,
                profile.Email,
                profile.MobileNumber,
                profile.PANNumber,
                profile.Address,
                // Date-only value: sent as plain yyyy-MM-dd so the browser can't
                // shift it by a timezone.
                DateOfBirth = profile.DateOfBirth?.ToString("yyyy-MM-dd"),
                profile.IsLocked,
                profile.FailedLoginAttempts,
                LastLoginDate = AsUtc(profile.LastLoginDate),
                CreatedDate = AsUtc(profile.CreatedDate)
            });
        }

        /// <summary>Officer profile — dbo.Officers.</summary>
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
                    o.LastLoginDate,
                    o.CreatedDate,
                    o.CreatedBy
                })
                .FirstOrDefaultAsync();

            if (profile == null) return NotFound();

            return Json(new
            {
                profile.OfficerId,
                profile.Username,
                profile.FullName,
                profile.Department,
                profile.Designation,
                profile.Email,
                profile.IsLocked,
                profile.FailedLoginAttempts,
                LastLoginDate = AsUtc(profile.LastLoginDate),
                CreatedDate = AsUtc(profile.CreatedDate),
                profile.CreatedBy
            });
        }

        // Login writes DateTime.UtcNow, but SQL Server hands the value back with
        // Kind = Unspecified. Tagging it as UTC makes it serialise with a "Z", so
        // the browser shows it in the viewer's local time instead of raw UTC.
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

}
