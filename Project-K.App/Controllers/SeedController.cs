using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Project_K.BusinessLogic.Interfaces;
using Project_K.Infrastructure.Models;
using Microsoft.AspNetCore.Identity;

namespace Project_K.Controllers
{

    [ServiceFilter(typeof(CheckDatabaseStateFilter))]
    public class SeedController : Controller
    {
        private readonly IAccountService _accountService;
        private readonly IRoleService _roleService;
        private readonly UserManager<User> _userManager;
        public SeedController(IAccountService accountService, IRoleService roleService, UserManager<User> userManager)
        {
            _roleService = roleService;
            _accountService = accountService;
            _userManager = userManager;
        }
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult SeedAdmin()
        {
            return View();
        }
        
        [HttpPost]
        public async Task<IActionResult> SeedAdmin(string email, string password, string passwordConfirm)
        {
            if (ModelState.IsValid)
            {
                if (password != passwordConfirm)
                {
                    ModelState.AddModelError(string.Empty, "Паролі не співпадають");
                    return View();
                }
                var user = new User { Email = email, UserName = email };
                var result = await _accountService.CreateAccount(user, password);
                if (result.Succeeded)
                {
                    await _roleService.CreateRole("Admin");
                    await _userManager.AddToRoleAsync(user, "Admin");
                    return RedirectToAction("SeedOtherData");
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return View();
                }
            }
            return View();
        }

        [HttpGet]
        public IActionResult SeedOtherData()
        {
            return View();
        }
    }
}
