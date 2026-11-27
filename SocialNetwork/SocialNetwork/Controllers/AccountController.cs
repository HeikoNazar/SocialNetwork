using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SocialNetwork.Data;
using SocialNetwork.Data.Models;
using SocialNetwork.Data.Services;
using SocialNetwork.Extensions;
using SocialNetwork.ViewModels.Account;

namespace SocialNetwork.Controllers
{
    public class AccountController : Controller
    {
        private const long MaxPictureSize = 2 * 1024 * 1024; // 2 MB
        private static readonly string[] AllowedPictureExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

        private readonly AppDBContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IPostService _postService;
        private readonly IWebHostEnvironment _environment;

        public AccountController(AppDBContext context, IPasswordHasher<User> passwordHasher, IPostService postService, IWebHostEnvironment environment)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _postService = postService;
            _environment = environment;
        }

        [Authorize]
        public async Task<IActionResult> Index()
        {
            int userId = User.GetUserId();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);
            if (user == null)
            {
                // The account no longer exists, so end the session
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction(nameof(Login));
            }

            var model = new ProfileVM
            {
                FullName = user.FullName,
                UserHandle = user.UserHandle,
                ProfilePictureUrl = user.ProfilePictureUrl,
                SubscribersCount = await _context.Subscriptions.CountAsync(s => s.SubscribedToId == userId),
                SubscriptionsCount = await _context.Subscriptions.CountAsync(s => s.SubscriberId == userId),
                Posts = await _postService.GetUserPostsAsync(userId)
            };

            return View(model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(5 * 1024 * 1024)]
        public async Task<IActionResult> UploadPicture(IFormFile? picture)
        {
            if (picture == null || picture.Length == 0)
            {
                TempData["PictureError"] = "Choose an image first.";
                return RedirectToAction(nameof(Index));
            }

            if (picture.Length > MaxPictureSize)
            {
                TempData["PictureError"] = "The image is too big. The maximum size is 2 MB.";
                return RedirectToAction(nameof(Index));
            }

            var extension = Path.GetExtension(picture.FileName).ToLowerInvariant();
            if (!AllowedPictureExtensions.Contains(extension) || !await HasImageSignatureAsync(picture, extension))
            {
                TempData["PictureError"] = "Please upload a JPG, PNG, GIF or WebP image.";
                return RedirectToAction(nameof(Index));
            }

            int userId = User.GetUserId();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);
            if (user == null)
            {
                return RedirectToAction(nameof(Login));
            }

            var folder = Path.Combine(_environment.WebRootPath, "images", "profiles");
            Directory.CreateDirectory(folder);

            var fileName = Guid.NewGuid() + extension;
            using (var stream = new FileStream(Path.Combine(folder, fileName), FileMode.Create))
            {
                await picture.CopyToAsync(stream);
            }

            // Remove the previous uploaded picture (never touches the default image)
            var oldUrl = user.ProfilePictureUrl;
            user.ProfilePictureUrl = "/images/profiles/" + fileName;
            await _context.SaveChangesAsync();

            if (oldUrl != null && oldUrl.StartsWith("/images/profiles/"))
            {
                var oldPath = Path.Combine(folder, Path.GetFileName(oldUrl));
                if (System.IO.File.Exists(oldPath))
                {
                    System.IO.File.Delete(oldPath);
                }
            }

            // Sign in again so the header shows the new picture straight away
            await SignInUserAsync(user);

            TempData["PictureSuccess"] = "Profile picture updated.";
            return RedirectToAction(nameof(Index));
        }

        // Checks the first bytes of the file, so a renamed non-image file is rejected
        private static async Task<bool> HasImageSignatureAsync(IFormFile file, string extension)
        {
            var header = new byte[12];
            using var stream = file.OpenReadStream();
            int read = await stream.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false);
            if (read < header.Length)
            {
                return false;
            }

            return extension switch
            {
                ".jpg" or ".jpeg" => header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
                ".png" => header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47,
                ".gif" => header[0] == 0x47 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x38,
                ".webp" => header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46
                           && header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50,
                _ => false
            };
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

            await SignInUserAsync(user);

            // Only follow local return URLs, otherwise anyone could bounce users to another site.
            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return LocalRedirect(model.ReturnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            return View(new RegisterVM());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterVM model)
        {
            model.FullName = model.FullName?.Trim() ?? string.Empty;
            model.UserHandle = model.UserHandle?.Trim() ?? string.Empty;
            model.Email = model.Email?.Trim() ?? string.Empty;

            if (ModelState.IsValid)
            {
                if (await _context.Users.AnyAsync(u => u.Email == model.Email))
                {
                    ModelState.AddModelError(nameof(model.Email), "An account with this email already exists");
                }

                if (await _context.Users.AnyAsync(u => u.UserHandle == model.UserHandle))
                {
                    ModelState.AddModelError(nameof(model.UserHandle), "This username is already taken");
                }
            }

            if (!ModelState.IsValid)
            {
                model.Password = string.Empty;
                model.ConfirmPassword = string.Empty;
                return View(model);
            }

            var user = new User
            {
                FullName = model.FullName,
                UserHandle = model.UserHandle,
                Email = model.Email
            };
            user.PasswordHash = _passwordHasher.HashPassword(user, model.Password);

            try
            {
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Most likely someone registered the same email at the same moment (unique index on Email).
                ModelState.AddModelError(string.Empty, "Could not create the account. Please try again.");
                model.Password = string.Empty;
                model.ConfirmPassword = string.Empty;
                return View(model);
            }

            await SignInUserAsync(user);
            return RedirectToAction("Index", "Home");
        }

        private Task SignInUserAsync(User user)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.FullName),
                new(ClaimTypes.Email, user.Email!),
                new(ClaimsPrincipalExtensions.UserHandleClaim, user.UserHandle)
            };
            if (!string.IsNullOrEmpty(user.ProfilePictureUrl))
            {
                claims.Add(new Claim(ClaimsPrincipalExtensions.ProfilePictureClaim, user.ProfilePictureUrl));
            }

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            return HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity));
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
