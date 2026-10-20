using LostAndFound.Core.Domain.IdentityEntities;
using LostAndFound.Core.DTO;
using LostAndFound.Ui.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LostAndFound.Ui.Controllers
{
    public class AccountController : Controller
    {
        private static readonly string[] EgyptianCities =
        [
            "Alexandria", "Aswan", "Asyut", "Beheira", "Beni Suef", "Cairo", "Dakahlia",
            "Damietta", "Fayoum", "Gharbia", "Giza", "Ismailia", "Kafr El Sheikh",
            "Luxor", "Matruh", "Menofia", "Minya", "New Valley", "North Sinai",
            "Port Said", "Qalyubia", "Qena", "Red Sea", "Sharqia", "Sohag",
            "South Sinai", "Suez"
        ];

        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        [HttpGet]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            ViewBag.EgyptianCities = EgyptianCities;
            return View(new ProfileViewModel
            {
                PersonName = user.PersonName ?? string.Empty,
                City = user.City ?? string.Empty,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            ViewBag.EgyptianCities = EgyptianCities;
            model.Email = user.Email ?? string.Empty;
            model.PhoneNumber = user.PhoneNumber ?? string.Empty;

            if (!EgyptianCities.Contains(model.City, StringComparer.Ordinal))
                ModelState.AddModelError(nameof(model.City), "Choose a city in Egypt from the list.");

            if (!ModelState.IsValid) return View(model);

            user.PersonName = model.PersonName.Trim();
            user.City = model.City;
            user.PhoneNumber = model.PhoneNumber.Trim();
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
                return View(model);
            }

            TempData["Success"] = "Your profile has been updated.";
            return RedirectToAction(nameof(Profile));
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterDTO());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterDTO model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                PersonName = model.PersonName
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Index", "Home");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginDTO());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginDTO model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(
                model.Email,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: false);

            if (result.Succeeded)
            {
                if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return LocalRedirect(returnUrl);
                }

                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}
