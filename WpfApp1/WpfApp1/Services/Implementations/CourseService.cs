using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Dapper;

public class CourseService : BaseRepository, ICourseService
{
    public async Task<List<Course>> GetAllCoursesAsync()
    {
        try
        {
            return (await QueryAsync<Course>("SELECT * FROM courses")).AsList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting courses: {ex.Message}");
            return new List<Course>();
        }
    }

    public async Task<Course> GetCourseByIdAsync(int courseId)
    {
        try
        {
            return await QueryFirstOrDefaultAsync<Course>(
                "SELECT * FROM courses WHERE course_id = @CourseId",
                new { CourseId = courseId });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting course: {ex.Message}");
            return null;
        }
    }

    public async Task<int> CreateCourseAsync(Course course)
    {
        try
        {
            var query = @"INSERT INTO courses (name, description, created_by, start_date, end_date)
                        VALUES (@Name, @Description, @CreatedBy, @StartDate, @EndDate)
                        RETURNING course_id";
            return await ExecuteScalarAsync<int>(query, course);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating course: {ex.Message}");
            return -1;
        }
    }

    public async Task<bool> UpdateCourseAsync(Course course)
    {
        try
        {
            var query = @"UPDATE courses SET 
                        name = @Name,
                        description = @Description,
                        start_date = @StartDate,
                        end_date = @EndDate
                        WHERE course_id = @CourseId";

            var affected = await ExecuteAsync(query, course);
            return affected > 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating course: {ex.Message}");
            return false;
        }
    }

   

    public async Task<List<Enrollment>> GetEnrollmentsByUserAsync(int userId)
    {
        try
        {
            return (await QueryAsync<Enrollment>(
                "SELECT * FROM enrollments WHERE user_id = @UserId",
                new { UserId = userId })).AsList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting enrollments: {ex.Message}");
            return new List<Enrollment>();
        }
    }

    public async Task<bool> EnrollUserToCourseAsync(int userId, int courseId)
    {
        try
        {
            var affected = await ExecuteAsync(
                @"INSERT INTO enrollments (user_id, course_id, status)
                VALUES (@UserId, @CourseId, 'active')",
                new { UserId = userId, CourseId = courseId });
            return affected > 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error enrolling user: {ex.Message}");
            return false;
        }
    }
}