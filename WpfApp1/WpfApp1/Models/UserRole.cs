using System.ComponentModel.DataAnnotations.Schema;

public class UserRole
{
    [Column("user_role_id")]
    public int UserRoleId { get; set; }

    [Column("user_id")]
    public int UserId { get; set; }

    [Column("role_id")]
    public int RoleId { get; set; }
}