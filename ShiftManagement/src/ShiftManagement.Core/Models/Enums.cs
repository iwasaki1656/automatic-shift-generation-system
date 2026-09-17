namespace ShiftManagement.Core.Models;

/// <summary>勤務カテゴリ</summary>
public enum ShiftCategory
{
    /// <summary>休み</summary>
    Off,
    /// <summary>日勤</summary>
    Day,
    /// <summary>早出</summary>
    Early,
    /// <summary>遅出</summary>
    Late,
    /// <summary>夜勤入り</summary>
    NightIn,
    /// <summary>夜勤明け</summary>
    NightOff,
    /// <summary>有給</summary>
    PaidOff,
    /// <summary>その他</summary>
    Other
}

/// <summary>制約レベル</summary>
public enum ConstraintLevel
{
    /// <summary>絶対条件（違反不可）</summary>
    Hard,
    /// <summary>個人的要望（できれば守る）</summary>
    Soft
}

/// <summary>制約タイプ</summary>
public enum ConstraintType
{
    // --- 不可系 ---
    /// <summary>希望休（特定日)</summary>
    DayOffRequest,
    /// <summary>勤務不可（特定日）</summary>
    WorkUnavailable,
    /// <summary>夜勤不可</summary>
    NightUnavailable,
    /// <summary>2階勤務不可</summary>
    Floor2Unavailable,
    /// <summary>3階勤務不可</summary>
    Floor3Unavailable,
    /// <summary>早出不可</summary>
    EarlyUnavailable,
    /// <summary>遅出不可</summary>
    LateUnavailable,
    /// <summary>日勤不可</summary>
    DayUnavailable,

    // --- 希望系 ---
    /// <summary>日勤希望（特定日）</summary>
    DayPreference,
    /// <summary>早出希望（特定日）</summary>
    EarlyPreference,
    /// <summary>遅出希望（特定日）</summary>
    LatePreference,
    /// <summary>夜勤希望（特定日）</summary>
    NightPreference,
    /// <summary>2階希望</summary>
    Floor2Preference,
    /// <summary>3階希望</summary>
    Floor3Preference,

    // --- 特殊ルール ---
    /// <summary>長岡妙子：希望日のみ2階夜勤</summary>
    NagaokaSpecialRule,
    /// <summary>特定シフト種類のみ可能</summary>
    AllowedShiftsOnly,
}

/// <summary>勤務表ステータス</summary>
public enum ScheduleStatus
{
    /// <summary>下書き（未生成）</summary>
    Draft,
    /// <summary>生成済み</summary>
    Generated,
    /// <summary>確定</summary>
    Finalized
}

/// <summary>検証違反の重大度</summary>
public enum ViolationSeverity
{
    /// <summary>情報</summary>
    Info,
    /// <summary>警告</summary>
    Warning,
    /// <summary>違反（Soft Constraint）</summary>
    SoftViolation,
    /// <summary>エラー（Hard Constraint違反）</summary>
    HardViolation
}
