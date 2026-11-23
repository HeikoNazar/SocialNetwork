using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SocialNetwork.Data;
using SocialNetwork.Data.Models;
using SocialNetwork.Extensions;
using SocialNetwork.ViewModels.Account;

namespace SocialNetwork.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDBContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;

        public AccountController(AppDBContext context, IPasswordHasher<User> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            return View(new LoginVM { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginVM model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var email = model.Email.Trim();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted);

            var verification = user?.PasswordHash == null
                ? PasswordVerificationResult.Failed
                : _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, model.Password);

            if (user == null || verification == PasswordVerificationResult.Failed)
            {
                // Same message for "unknown email" and "wrong password" on purpose.
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                model.Password = string.Empty;
                return View(model);
            }

            if (verification == PasswordVerificationResult.Success)
            {
                user.PasswordHash = _passwordHasher.HashPassword(user, model.Password);
                await _context.SaveChangesAsync();
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.FullName),
                new(ClaimTypes.Email, user.Email!),
                new(ClaimsPrincipalExtensions.UserHandleClaim, user.UserHandle)
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity));

            // Only follow local return URLs, otherwise anyone could bounce users to another site.
            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return LocalRedirect(model.ReturnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }
    }
}
