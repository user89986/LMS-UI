using System.Collections.Generic;
using System.Threading.Tasks;

public interface IUserService
{
    Task<List<User>> GetAllUsersAsync();
    Task<User> GetUserByIdAsync(int userId);
    Task<int> CreateUserAsync(User user);
    Task<bool> UpdateUserAsync(User user);
    Task<bool> DeleteUserAsync(int userId);
    Task<List<UserRole>> GetUserRolesAsync(int userId);
    Task<bool> AssignRoleToUserAsync(int userId, int roleId);
}