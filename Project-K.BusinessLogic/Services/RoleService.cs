using Microsoft.AspNetCore.Identity;
using Project_K.BusinessLogic.Interfaces;

namespace Project_K.BusinessLogic.Services
{
    public class RoleService : IRoleService
    {
        private readonly RoleManager<IdentityRole> _roleManager;

        public RoleService(RoleManager<IdentityRole> roleManager)
        {
            _roleManager = roleManager;
        }

        public async Task<IEnumerable<IdentityRole>> GetRoles()
        {
            return await Task.Run(() => _roleManager.Roles.ToList());
        }

        public async Task<IdentityResult> CreateRole(string name)
        {
            var role = new IdentityRole(name);
            return await _roleManager.CreateAsync(role);
        }

        public async Task<IdentityResult> DeleteRole(string id)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role == null) return IdentityResult.Failed(new IdentityError { Description = "Роль не знайдено" });
            else return await _roleManager.DeleteAsync(role);
        }
    }
}