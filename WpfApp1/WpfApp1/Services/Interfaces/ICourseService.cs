using System.Collections.Generic;
using System.Threading.Tasks;

public interface ICourseService
{
    Task<List<Course>> GetAllCoursesAsync();
    Task<Course> GetCourseByIdAsync(int courseId);
    Task<int> CreateCourseAsync(Course course);
    Task<bool> UpdateCourseAsync(Course course);
    Task<List<Enrollment>> GetEnrollmentsByUserAsync(int userId);
    Task<bool> EnrollUserToCourseAsync(int userId, int courseId);
}