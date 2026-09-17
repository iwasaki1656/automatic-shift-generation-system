namespace ShiftManagement.Core.Models;

/// <summary>職員</summary>
public class Staff
{
    public int Id { get; set; }
    public string LastName { get; set; } = "";
    public string FirstName { get; set; } = "";

    /// <summary>表示名（姓　名形式）</summary>
    public string DisplayName { get; set; } = "";

    public int JobTypeId { get; set; }
    public JobType? JobType { get; set; }

    public int EmploymentTypeId { get; set; }
    public EmploymentType? EmploymentType { get; set; }

    public int? QualificationId { get; set; }
    public Qualification? Qualification { get; set; }

    /// <summary>有効な職員か</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>表示順（Excel出力順）</summary>
    public int DisplayOrder { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>固定の個人条件</summary>
    public List<StaffPermanentConstraint> PermanentConstraints { get; set; } = new();

    // 識別用（個人条件チェック用）
    /// <summary>長岡妙子か（特殊ルール適用）</summary>
    public bool IsNagaokaTaeko => DisplayName.Contains("長岡") && DisplayName.Contains("妙子");

    /// <summary>上松伸次か（2階のみ）</summary>
    public bool IsUematsuShinji => DisplayName.Contains("上松") && DisplayName.Contains("伸次");

    /// <summary>三吉孝明か（3階早出・遅出のみ）</summary>
    public bool IsMiyoshiTakaaki => DisplayName.Contains("三吉") && DisplayName.Contains("孝明");

    /// <summary>松浦洋子か（日勤優先、夜勤不可）</summary>
    public bool IsMatsuuraYoko => DisplayName.Contains("松浦") && DisplayName.Contains("洋子");

    /// <summary>豊福有美か（日勤フォロー・2階夜勤のみ）</summary>
    public bool IsToyofukuYumi => DisplayName.Contains("豊福") && DisplayName.Contains("有美");
}
