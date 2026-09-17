namespace ShiftManagement.Core.Models;

/// <summary>勤務形態</summary>
public class EmploymentType
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public int DisplayOrder { get; set; }

    public static readonly string FullTimeCode = "FULL_TIME";
    public static readonly string PartTimeCode = "PART_TIME";
    public static readonly string NightOnlyCode = "NIGHT_ONLY";
}
