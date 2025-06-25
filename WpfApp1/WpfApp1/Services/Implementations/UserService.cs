using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Dapper;

public class UserService : BaseRepository, IUserService
{
    public async Task<List<User>> GetAllUsersAsync()
    {
        try
        {
            return (await QueryAsync<User>("SELECT * FROM users")).AsList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting users: {ex.Message}");
            return new List<User>();
        }
    }

    public async Task<User> GetUserByIdAsync(int userId)
    {
        try
        {
            return await QueryFirstOrDefaultAsync<User>(
                "SELECT * FROM users WHERE user_id = @UserId",
                new { UserId = userId });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting user: {ex.Message}");
            return null;
        }
    }

    public async Task<int> CreateUserAsync(User user)
    {
        try
        {
            var query = @"INSERT INTO users (login, email, password_hash, first_name, last_name) 
                        VALUES (@Login, @Email, @PasswordHash, @FirstName, @LastName)
                        RETURNING user_id";
            return await ExecuteScalarAsync<int>(query, user);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating user: {ex.Message}");
            return -1;
        }
    }

    public async Task<bool> UpdateUserAsync(User user)
    {
        try
        {
            var query = @"UPDATE users SET 
                        login = @Login, 
                        email = @Email,
                        first_name = @FirstName,
                        last_name = @LastName
                        WHERE user_id = @UserId";

            var affected = await ExecuteAsync(query, user);
            return affected > 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating user: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> DeleteUserAsync(int userId)
    {
        try
        {
            var affected = await ExecuteAsync(
                "DELETE FROM users WHERE user_id = @UserId",
                new { UserId = userId });
            return affected > 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting user: {ex.Message}");
            return false;
        }
    }

    public async Task<List<UserRole>> GetUserRolesAsync(int userId)
    {
        try
        {
            return (await QueryAsync<UserRole>(
                "SELECT * FROM user_roles WHERE user_id = @UserId",
                new { UserId = userId })).AsList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting user roles: {ex.Message}");
            return new List<UserRole>();
        }
    }

    public async Task<bool> AssignRoleToUserAsync(int userId, int roleId)
    {
        try
        {
            var affected = await ExecuteAsync(
                "INSERT INTO user_roles (user_id, role_id) VALUES (@UserId, @RoleId)",
                new { UserId = userId, RoleId = roleId });
            return affected > 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error assigning role: {ex.Message}");
            return false;
        }
    }
}