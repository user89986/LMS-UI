using System.ComponentModel.DataAnnotations.Schema;

public class Enrollment
{
    [Column("enrollment_id")] public int EnrollmentId { get; set; }
    [Column("user_id")] public int UserId { get; set; }
    [Column("course_id")] public int CourseId { get; set; }
    [Column("enrolled_at")] public DateTime EnrolledAt { get; set; }
    [Column("completed_at")] public DateTime? CompletedAt { get; set; }
    [Column("status")] public string Status { get; set; }
}