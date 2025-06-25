using System.ComponentModel.DataAnnotations.Schema;

public class Course
{
    [Column("course_id")]
    public int CourseId { get; set; }

    [Column("name")]
    public string Name { get; set; }

    [Column("description")]
    public string Description { get; set; }

    [Column("created_by")]
    public int CreatedBy { get; set; }

    [Column("start_date")]
    public DateTime StartDate { get; set; }

    [Column("end_date")]
    public DateTime EndDate { get; set; }
}