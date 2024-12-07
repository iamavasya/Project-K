using Microsoft.AspNetCore.Identity;
using Project_K.Infrastructure.Models;
using Project_K.BusinessLogic.Interfaces;

namespace Project_K.Services
{
    public class AccountService : IAccountService
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        public AccountService(UserManager<User> userManager, SignInManager<User> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public async Task<IdentityResult> CreateAccount(User user, string password)
        {
            var result = await _userManager.CreateAsync(user, password);
            await _signInManager.SignInAsync(user, false);
            return result;
        }
    }
}