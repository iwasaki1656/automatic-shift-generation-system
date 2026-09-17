using ShiftManagement.Core.Models;

namespace ShiftManagement.Core.Interfaces;

/// <summary>勤務表最適化エンジンのインターフェース</summary>
public interface IShiftOptimizer
{
    /// <summary>
    /// 勤務表を最適化して3案を生成する
    /// </summary>
    Task<OptimizationResult> OptimizeAsync(
        OptimizationInput input,
        IProgress<OptimizationProgress>? progress = null,
        CancellationToken cancellationToken = default);
}

/// <summary>最適化入力データ</summary>
public class OptimizationInput
{
    public MonthlySchedule Schedule { get; set; } = null!;
    public List<Staff> AllStaff { get; set; } = new();
    public List<ShiftType> ShiftTypes { get; set; } = new();
    public List<ShiftRequirement> Requirements { get; set; } = new();
    public List<PersonalConstraint> PersonalConstraints { get; set; } = new();
    public List<NightDutyTarget> NightDutyTargets { get; set; } = new();
    public FacilitySettings Settings { get; set; } = null!;

    /// <summary>前月末の夜勤情報（月末繰越用）</summary>
    public List<ShiftAssignment>? PreviousMonthTrailingAssignments { get; set; }
}

/// <summary>最適化結果</summary>
public class OptimizationResult
{
    public bool IsSuccess { get; set; }
    public List<ScheduleCase> Cases { get; set; } = new();

    /// <summary>解なし時の競合分析</summary>
    public ConflictAnalysis? ConflictAnalysis { get; set; }

    /// <summary>エラーメッセージ（失敗時）</summary>
    public string? ErrorMessage { get; set; }

    public double TotalTimeSeconds { get; set; }
}

/// <summary>最適化の進捗情報</summary>
public class OptimizationProgress
{
    public int CurrentCase { get; set; }
    public int TotalCases { get; set; }
    public string Message { get; set; } = "";
    public double ProgressPercentage { get; set; }
}
