namespace ShiftManagement.Core.Models;

/// <summary>月間勤務表</summary>
public class MonthlySchedule
{
    public int Id { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public ScheduleStatus Status { get; set; } = ScheduleStatus.Draft;

    /// <summary>前月の勤務表ID（夜勤月末繰越のため）</summary>
    public int? PreviousMonthScheduleId { get; set; }
    public MonthlySchedule? PreviousMonthSchedule { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>生成された勤務表案</summary>
    public List<ScheduleCase> Cases { get; set; } = new();

    /// <summary>この月の個人制約</summary>
    public List<PersonalConstraint> PersonalConstraints { get; set; } = new();

    /// <summary>対象月の日数</summary>
    public int DaysInMonth => DateTime.DaysInMonth(Year, Month);

    /// <summary>対象月の全日付</summary>
    public IEnumerable<DateOnly> AllDates =>
        Enumerable.Range(1, DaysInMonth).Select(d => new DateOnly(Year, Month, d));

    public override string ToString() => $"{Year}年{Month}月 勤務表";
}

/// <summary>勤務表案（3案のうち1つ）</summary>
public class ScheduleCase
{
    public int Id { get; set; }
    public int MonthlyScheduleId { get; set; }
    public MonthlySchedule? MonthlySchedule { get; set; }

    /// <summary>案番号（1,2,3）</summary>
    public int CaseNumber { get; set; }

    /// <summary>案名（例:「総合評価最大化案」）</summary>
    public string CaseName { get; set; } = "";

    /// <summary>説明（案の特徴）</summary>
    public string? Description { get; set; }

    /// <summary>総合スコア（0-100）</summary>
    public double? TotalScore { get; set; }

    public int HardViolationCount { get; set; } = 0;
    public int SoftViolationCount { get; set; } = 0;
    public double? PersonalWishAchievementRate { get; set; }
    public double? NightDutyVariance { get; set; }
    public double? WorkloadVariance { get; set; }

    /// <summary>採用された案か</summary>
    public bool IsSelected { get; set; } = false;

    /// <summary>ソルバーの実行時間（秒）</summary>
    public double? SolverTimeSeconds { get; set; }

    public DateTime CreatedAt { get; set; }

    /// <summary>勤務割り当て</summary>
    public List<ShiftAssignment> Assignments { get; set; } = new();

    /// <summary>個人別評価</summary>
    public List<StaffEvaluation> StaffEvaluations { get; set; } = new();

    /// <summary>バリデーション結果</summary>
    public ValidationResultData? ValidationResult { get; set; }
}

/// <summary>勤務割り当て（1件=1職員×1日の勤務）</summary>
public class ShiftAssignment
{
    public int Id { get; set; }
    public int ScheduleCaseId { get; set; }
    public int StaffId { get; set; }
    public Staff? Staff { get; set; }
    public DateOnly Date { get; set; }
    public int ShiftTypeId { get; set; }
    public ShiftType? ShiftType { get; set; }

    /// <summary>管理者による手動修正か</summary>
    public bool IsManuallyModified { get; set; } = false;
    public DateTime? ModifiedAt { get; set; }
}

/// <summary>個人別評価結果</summary>
public class StaffEvaluation
{
    public int Id { get; set; }
    public int ScheduleCaseId { get; set; }
    public int StaffId { get; set; }
    public Staff? Staff { get; set; }

    public int WorkDayCount { get; set; }
    public int DayOffCount { get; set; }
    public int NightDutyCount { get; set; }
    public int EarlyCount { get; set; }
    public int LateCount { get; set; }
    public int DayCount { get; set; }
    public int Floor2Count { get; set; }
    public int Floor3Count { get; set; }
    public double WorkloadScore { get; set; }

    public int SoftAchievedCount { get; set; }
    public int SoftViolationCount { get; set; }

    /// <summary>未達成詳細（JSON）</summary>
    public string? ViolationDetailsJson { get; set; }
}

/// <summary>バリデーション結果データ</summary>
public class ValidationResultData
{
    public int Id { get; set; }
    public int ScheduleCaseId { get; set; }
    public DateTime ValidatedAt { get; set; }
    public bool IsValid { get; set; }

    /// <summary>Hard制約違反（JSON）</summary>
    public string? HardViolationsJson { get; set; }

    /// <summary>Soft制約未達成（JSON）</summary>
    public string? SoftViolationsJson { get; set; }
    public string? Summary { get; set; }
}
