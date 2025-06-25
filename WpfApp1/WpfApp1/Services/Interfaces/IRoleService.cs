using System.Collections.Generic;
using System.Threading.Tasks;

public interface IRoleService
{
    Task<List<Role>> GetAllRolesAsync();
    Task<Role> GetRoleByIdAsync(int roleId);
}