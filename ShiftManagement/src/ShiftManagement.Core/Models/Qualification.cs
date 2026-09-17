namespace ShiftManagement.Core.Models;

/// <summary>資格</summary>
public class Qualification
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public int DisplayOrder { get; set; }
}
