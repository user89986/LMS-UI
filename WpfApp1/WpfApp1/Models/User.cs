using System.ComponentModel.DataAnnotations.Schema;

public class User
{
    [Column("user_id")]
    public int UserId { get; set; }

    [Column("login")]
    public string Login { get; set; }

    [Column("email")]
    public string Email { get; set; }

    [Column("first_name")]
    public string FirstName { get; set; }

    [Column("last_name")]
    public string LastName { get; set; }

    [Column("last_login")]
    public DateTime? LastLogin { get; set; }
}