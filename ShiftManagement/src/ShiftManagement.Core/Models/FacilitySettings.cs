namespace ShiftManagement.Core.Models;

/// <summary>施設設定</summary>
public class FacilitySettings
{
    public int Id { get; set; } = 1;
    public string FacilityName { get; set; } = "ライフステイむなかた";

    /// <summary>月間最低休日数</summary>
    public int MinDaysOff { get; set; } = 9;

    /// <summary>最大連続勤務日数</summary>
    public int MaxConsecutiveWorkDays { get; set; } = 5;

    /// <summary>夜勤明けを休日としてカウントするか</summary>
    public bool CountNightOffAsRest { get; set; } = false;

    /// <summary>夜勤負担度</summary>
    public double NightDutyLoadFactor { get; set; } = 3.0;

    /// <summary>早出負担度</summary>
    public double EarlyDutyLoadFactor { get; set; } = 2.0;

    /// <summary>遅出負担度</summary>
    public double LateDutyLoadFactor { get; set; } = 2.0;

    /// <summary>日勤負担度</summary>
    public double DayDutyLoadFactor { get; set; } = 1.0;

    /// <summary>OR-Toolsソルバーのタイムアウト（秒）</summary>
    public int SolverTimeoutSeconds { get; set; } = 60;

    /// <summary>ソルバー生成案の最大数</summary>
    public int MaxCaseCount { get; set; } = 3;

    public DateTime UpdatedAt { get; set; }
}

/// <summary>各勤務種類の1日の必要人数</summary>
public class ShiftRequirement
{
    public int Id { get; set; }
    public int ShiftTypeId { get; set; }
    public ShiftType? ShiftType { get; set; }
    public int RequiredCount { get; set; } = 1;
    public bool IsActive { get; set; } = true;
}
