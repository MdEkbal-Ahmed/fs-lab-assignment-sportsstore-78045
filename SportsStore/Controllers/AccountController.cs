using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SportsStore.Models.ViewModels;

namespace SportsStore.Controllers {

    public class AccountController : Controller {
        private UserManager<IdentityUser> userManager;
        private SignInManager<IdentityUser> signInManager;
        private readonly ILogger<AccountController> logger;

        public AccountController(UserManager<IdentityUser> userMgr,
                SignInManager<IdentityUser> signInMgr,
                ILogger<AccountController> logger) {
            userManager = userMgr;
            signInManager = signInMgr;
            this.logger = logger;
        }

        public ViewResult Login(string returnUrl) {
            return View(new LoginModel {
                ReturnUrl = returnUrl
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginModel loginModel) {
            if (ModelState.IsValid && !string.IsNullOrEmpty(loginModel.Name) && loginModel.Password != null) {
                IdentityUser? user =
                    await userManager.FindByNameAsync(loginModel.Name);
                if (user != null) {
                    await signInManager.SignOutAsync();
                    var result = await signInManager.PasswordSignInAsync(user,
                            loginModel.Password, false, false);
                    if (result.Succeeded) {
                        logger.LogInformation(
                            "User login succeeded. Username: {Username}",
                            loginModel.Name);
                        return Redirect(loginModel?.ReturnUrl ?? "/Admin");
                    }
                }
                logger.LogWarning(
                    "User login failed. Username: {Username}",
                    loginModel.Name);
                ModelState.AddModelError("", "Invalid name or password");
            }
            return View(loginModel);
        }

        [Authorize]
        public async Task<RedirectResult> Logout(string returnUrl = "/") {
            var userName = User?.Identity?.Name ?? "Unknown";
            await signInManager.SignOutAsync();
            logger.LogInformation(
                "User logged out. Username: {Username}",
                userName);
            return Redirect(returnUrl);
        }
    }
}
