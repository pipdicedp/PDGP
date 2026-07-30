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

        private const int MaxFailedAttempts = 5;

        public AccountController(ApplicationDbContext context, CaptchaService captchaService)
        {
            _context = context;
            _captchaService = captchaService;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            // ---- 1. CAPTCHA check FIRST, before touching the database at all ----
            // This is what actually stops scripted brute-force attempts: a bot
            // has to solve a fresh CAPTCHA on every single POST, so credential-
            // stuffing at scale becomes impractical even before rate limiting.
            var expectedCode = HttpContext.Session.GetString("CaptchaCode");
            HttpContext.Session.Remove("CaptchaCode"); // single-use, whether it matches or not

            if (string.IsNullOrEmpty(expectedCode) ||
                !string.Equals(model.CaptchaInput?.Trim(), expectedCode, StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(model.CaptchaInput), "The code entered does not match the image. Please try again.");
                return View(model);
            }

            if (!ModelState.IsValid) return View(model);

            // ---- 2. Look up the user ----
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == model.Username);

            // Deliberately generic error message for both "no such user" and "wrong
            // password" — a distinct message for each would let an attacker enumerate
            // valid usernames.
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

            var verifyResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, model.Password);
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
        /// Generates a fresh CAPTCHA code each time it's requested, stores the
        /// code server-side in Session, and returns an SVG image of it. The
        /// login page's "refresh" icon just reloads this <img> with a cache-buster.
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

        /// <summary>
        /// DEVELOPMENT ONLY — creates one working test login so you can try the
        /// page before a Register module exists. Guarded by environment check;
        /// remove this action entirely before deploying anywhere non-Development.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> SeedTestUser([FromServices] IWebHostEnvironment env)
        {
            if (!env.IsDevelopment()) return NotFound();

            if (await _context.Users.AnyAsync(u => u.Username == "testuser"))
                return Content("Test user already exists. Username: testuser / Password: Test@12345");

            var user = new ApplicationUser { Username = "testuser" };
            user.PasswordHash = _passwordHasher.HashPassword(user, "Test@12345");
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Content("Created. Username: testuser / Password: Test@12345");
        }

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
