using Microsoft.AspNetCore.Identity;
using Project_K.Infrastructure.Models;

namespace Project_K.BusinessLogic.Interfaces
{
    public interface IAccountService
    {
        Task<IdentityResult> CreateAccount(User user, string password);
    }
}