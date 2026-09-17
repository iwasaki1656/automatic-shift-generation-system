namespace ShiftManagement.Core.Models;

/// <summary>職種</summary>
public class JobType
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public int DisplayOrder { get; set; }

    public static readonly string NursingCode = "NURSING";
    public static readonly string CareCode = "CARE";
}
