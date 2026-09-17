namespace ShiftManagement.Core.Models;

/// <summary>月間の個人勤務希望・制約</summary>
public class PersonalConstraint
{
    public int Id { get; set; }
    public int MonthlyScheduleId { get; set; }
    public int StaffId { get; set; }
    public Staff? Staff { get; set; }

    public ConstraintType Type { get; set; }

    /// <summary>対象日（nullの場合は月全体に適用）</summary>
    public DateOnly? TargetDate { get; set; }

    /// <summary>希望勤務種類ID（希望系のみ使用）</summary>
    public int? ShiftTypeId { get; set; }
    public ShiftType? ShiftType { get; set; }

    /// <summary>Hard制約かSoft制約か</summary>
    public ConstraintLevel Level { get; set; } = ConstraintLevel.Soft;

    /// <summary>優先度（1-5、Softのみ使用）</summary>
    public int Priority { get; set; } = 3;

    /// <summary>備考</summary>
    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>固定の個人条件（月に依存しない永続条件）</summary>
public class StaffPermanentConstraint
{
    public int Id { get; set; }
    public int StaffId { get; set; }
    public Staff? Staff { get; set; }

    public ConstraintType Type { get; set; }

    /// <summary>対象の勤務種類（AllowedShiftsOnlyの場合はこの種類のみ許可）</summary>
    public int? ShiftTypeId { get; set; }
    public ShiftType? ShiftType { get; set; }

    public ConstraintLevel Level { get; set; } = ConstraintLevel.Hard;
    public int Priority { get; set; } = 5;
    public string? Note { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>夜勤回数目標設定</summary>
public class NightDutyTarget
{
    public int Id { get; set; }
    public int MonthlyScheduleId { get; set; }
    public int StaffId { get; set; }
    public Staff? Staff { get; set; }

    public int MinCount { get; set; } = 0;
    public int? TargetCount { get; set; }
    public int? MaxCount { get; set; }
}

/// <summary>制約変更履歴</summary>
public class ConstraintChangeHistory
{
    public int Id { get; set; }
    public int? StaffId { get; set; }
    public int? ConstraintId { get; set; }

    /// <summary>変更種類（"HARD_TO_SOFT", "SOFT_TO_HARD", "CREATED", "DELETED"）</summary>
    public string ChangeType { get; set; } = "";
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public DateTime ChangedAt { get; set; }
    public string? ChangedBy { get; set; }
}
