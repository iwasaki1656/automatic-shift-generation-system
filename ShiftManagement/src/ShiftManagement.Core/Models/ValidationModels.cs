namespace ShiftManagement.Core.Models;

/// <summary>制約違反の詳細情報</summary>
public class ViolationDetail
{
    /// <summary>違反の重大度</summary>
    public ViolationSeverity Severity { get; set; }

    /// <summary>関連する職員ID</summary>
    public int? StaffId { get; set; }
    public string StaffName { get; set; } = "";

    /// <summary>関連する日付</summary>
    public DateOnly? Date { get; set; }

    /// <summary>実際の勤務コード</summary>
    public string? ActualShiftCode { get; set; }
    public string? ActualShiftName { get; set; }

    /// <summary>違反の種類コード</summary>
    public string ViolationCode { get; set; } = "";

    /// <summary>人間向けの説明</summary>
    public string Message { get; set; } = "";

    /// <summary>詳細説明（なぜこうなったか）</summary>
    public string? Detail { get; set; }

    /// <summary>関連する制約ID</summary>
    public int? ConstraintId { get; set; }

    /// <summary>ペナルティポイント（Soft違反のみ）</summary>
    public int PenaltyPoints { get; set; }

    /// <summary>Hard違反か</summary>
    public bool IsHardViolation => Severity == ViolationSeverity.HardViolation;
}

/// <summary>バリデーション結果</summary>
public class ValidationResult
{
    public bool IsValid => HardViolations.Count == 0;
    public List<ViolationDetail> HardViolations { get; set; } = new();
    public List<ViolationDetail> SoftViolations { get; set; } = new();
    public DateTime ValidatedAt { get; set; } = DateTime.Now;

    public int TotalPenalty => SoftViolations.Sum(v => v.PenaltyPoints);

    public string Summary => IsValid
        ? $"検証OK: Hard違反0件、Soft未達成{SoftViolations.Count}件"
        : $"Hard違反{HardViolations.Count}件、Soft未達成{SoftViolations.Count}件";
}

/// <summary>制約競合の分析結果（解なし時の診断情報）</summary>
public class ConflictAnalysis
{
    /// <summary>競合している日付・勤務・職員の説明</summary>
    public List<string> ConflictDescriptions { get; set; } = new();

    /// <summary>不足している勤務</summary>
    public List<ShortageInfo> Shortages { get; set; } = new();

    /// <summary>緩和することで解決できる可能性がある条件の提案</summary>
    public List<RelaxationSuggestion> RelaxationSuggestions { get; set; } = new();
}

/// <summary>人員不足情報</summary>
public class ShortageInfo
{
    public DateOnly Date { get; set; }
    public string ShiftTypeName { get; set; } = "";
    public int Required { get; set; }
    public int Available { get; set; }
    public List<string> BlockedReasons { get; set; } = new();
}

/// <summary>条件緩和の提案</summary>
public class RelaxationSuggestion
{
    public string StaffName { get; set; } = "";
    public DateOnly? Date { get; set; }
    public string ConstraintDescription { get; set; } = "";
    public string RelaxationAction { get; set; } = "";
    public string ExpectedBenefit { get; set; } = "";
}

/// <summary>セルの状態（UI表示用）</summary>
public class CellStatus
{
    public int StaffId { get; set; }
    public DateOnly Date { get; set; }
    public string ShiftCode { get; set; } = "";
    public string ShiftSymbol { get; set; } = "";
    public bool IsWishFulfilled { get; set; }
    public bool HasHardViolation { get; set; }
    public bool HasSoftViolation { get; set; }
    public bool IsManuallyModified { get; set; }
    public List<ViolationDetail> Violations { get; set; } = new();
    public string? WishDescription { get; set; }
    public string? ReasonDescription { get; set; }
}
