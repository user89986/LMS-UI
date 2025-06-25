using System.ComponentModel.DataAnnotations.Schema;

public class SystemSetting
{
    [Column("setting_id")] public int SettingId { get; set; }
    [Column("setting_name")] public string SettingName { get; set; }
    [Column("description")] public string Description { get; set; }
    [Column("updated_at")] public DateTime UpdatedAt { get; set; }
    [Column("updated_by")] public int? UpdatedBy { get; set; }
}
