namespace ShiftManagement.Core.Models;

/// <summary>勤務種類</summary>
public class ShiftType
{
    public int Id { get; set; }

    /// <summary>内部コード（例: "NIGHT_2F"）</summary>
    public string Code { get; set; } = "";

    /// <summary>Excel・画面表示記号（例: "2－"）</summary>
    public string DisplaySymbol { get; set; } = "";

    /// <summary>表示名（例: "2階夜勤入り"）</summary>
    public string DisplayName { get; set; } = "";

    /// <summary>対象フロア（null=フロア不問, 2=2階, 3=3階）</summary>
    public int? Floor { get; set; }

    /// <summary>勤務カテゴリ</summary>
    public ShiftCategory Category { get; set; }

    /// <summary>負担度（公平性計算用）</summary>
    public double LoadFactor { get; set; } = 1.0;

    /// <summary>勤務日としてカウントするか</summary>
    public bool IsWorkDay { get; set; } = true;

    /// <summary>休日としてカウントするか</summary>
    public bool IsDayOff => !IsWorkDay || Category == ShiftCategory.PaidOff;

    /// <summary>夜勤系勤務か（入り・明け）</summary>
    public bool IsNightRelated => Category == ShiftCategory.NightIn || Category == ShiftCategory.NightOff;

    /// <summary>表示順</summary>
    public int DisplayOrder { get; set; }

    /// <summary>Excelセル背景色（ARGB形式文字列、null=デフォルト）</summary>
    public string? ExcelBackgroundColor { get; set; }

    /// <summary>Excelセルフォント色</summary>
    public string? ExcelFontColor { get; set; }

    // 標準の内部コード定数
    public static readonly string OffCode = "OFF";
    public static readonly string DayCode = "DAY";
    public static readonly string Early2FCode = "EARLY_2F";
    public static readonly string Late2FCode = "LATE_2F";
    public static readonly string Night2FCode = "NIGHT_2F";
    public static readonly string Early3FCode = "EARLY_3F";
    public static readonly string Late3FCode = "LATE_3F";
    public static readonly string Night3FCode = "NIGHT_3F";
    public static readonly string NightOffCode = "NIGHT_OFF";
    public static readonly string PaidOffCode = "PAID_OFF";
    public static readonly string OfficeCode = "OFFICE";
}
