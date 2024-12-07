using Microsoft.AspNetCore.Identity;

namespace Project_K.BusinessLogic.Interfaces
{
    public interface IRoleService
    {
        Task<IEnumerable<IdentityRole>> GetRoles();
        Task<IdentityResult> CreateRole(string name);
        Task<IdentityResult> DeleteRole(string id);
    }
}