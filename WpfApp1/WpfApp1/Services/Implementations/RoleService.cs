using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Dapper;

public class RoleService : BaseRepository, IRoleService
{
    public async Task<List<Role>> GetAllRolesAsync()
    {
        try
        {
            return (await QueryAsync<Role>("SELECT * FROM roles")).AsList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting roles: {ex.Message}");
            return new List<Role>();
        }
    }

    public async Task<Role> GetRoleByIdAsync(int roleId)
    {
        try
        {
            return await QueryFirstOrDefaultAsync<Role>(
                "SELECT * FROM roles WHERE role_id = @RoleId",
                new { RoleId = roleId });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting role: {ex.Message}");
            return null;
        }
    }
}