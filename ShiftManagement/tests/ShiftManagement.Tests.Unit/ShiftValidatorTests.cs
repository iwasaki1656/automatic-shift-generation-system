using FluentAssertions;
using ShiftManagement.Core.Models;
using ShiftManagement.Validation;
using Xunit;

namespace ShiftManagement.Tests.Unit;

public class ShiftValidatorTests
{
    private readonly ShiftValidator _validator = new();

    private List<ShiftType> CreateTestShiftTypes()
    {
        return new List<ShiftType>
        {
            new() { Id = 1, Code = "OFF", DisplaySymbol = "ヤ", DisplayName = "休み", ShiftCategory = ShiftCategory.Off, LoadFactor = 0, IsWorkDay = false },
            new() { Id = 2, Code = "DAY", DisplaySymbol = "日", DisplayName = "日勤", ShiftCategory = ShiftCategory.Day, LoadFactor = 1, IsWorkDay = true },
            new() { Id = 3, Code = "EARLY_2F", DisplaySymbol = "2早", DisplayName = "2階早出", Floor = 2, ShiftCategory = ShiftCategory.Early, LoadFactor = 2, IsWorkDay = true },
            new() { Id = 4, Code = "LATE_2F", DisplaySymbol = "2オ", DisplayName = "2階遅出", Floor = 2, ShiftCategory = ShiftCategory.Late, LoadFactor = 2, IsWorkDay = true },
            new() { Id = 5, Code = "NIGHT_2F", DisplaySymbol = "2－", DisplayName = "2階夜勤入り", Floor = 2, ShiftCategory = ShiftCategory.NightIn, LoadFactor = 3, IsWorkDay = true },
            new() { Id = 6, Code = "EARLY_3F", DisplaySymbol = "3早", DisplayName = "3階早出", Floor = 3, ShiftCategory = ShiftCategory.Early, LoadFactor = 2, IsWorkDay = true },
            new() { Id = 7, Code = "LATE_3F", DisplaySymbol = "3オ", DisplayName = "3階遅出", Floor = 3, ShiftCategory = ShiftCategory.Late, LoadFactor = 2, IsWorkDay = true },
            new() { Id = 8, Code = "NIGHT_3F", DisplaySymbol = "3－", DisplayName = "3階夜勤入り", Floor = 3, ShiftCategory = ShiftCategory.NightIn, LoadFactor = 3, IsWorkDay = true },
            new() { Id = 9, Code = "NIGHT_OFF", DisplaySymbol = "－", DisplayName = "夜勤明け", ShiftCategory = ShiftCategory.NightOff, LoadFactor = 1, IsWorkDay = true },
            new() { Id = 10, Code = "PAID_OFF", DisplaySymbol = "有", DisplayName = "有給", ShiftCategory = ShiftCategory.PaidOff, LoadFactor = 0, IsWorkDay = false }
        };
    }

    [Fact]
    public void Validate_WhenNightSequenceBroken_ShouldReportHardViolation()
    {
        // 夜勤入りの翌日が日勤（夜勤明けでない）ケース
        var shiftTypes = CreateTestShiftTypes();
        var staff = new Staff { Id = 1, DisplayName = "山田　太郎" };

        var date1 = new DateOnly(2025, 3, 1);
        var date2 = new DateOnly(2025, 3, 2);

        var assignments = new List<ShiftAssignment>
        {
            new() { StaffId = 1, Date = date1, ShiftTypeId = 5 }, // 2階夜勤入り
            new() { StaffId = 1, Date = date2, ShiftTypeId = 2 }  // 日勤（違反！）
        };

        var input = new OptimizationInput
        {
            Schedule = new MonthlySchedule { Year = 2025, Month = 3 },
            AllStaff = new List<Staff> { staff },
            ShiftTypes = shiftTypes,
            Settings = new FacilitySettings { MinDaysOff = 0, MaxConsecutiveWorkDays = 10 }
        };

        var result = _validator.Validate(assignments, input);

        result.IsValid.Should().BeFalse();
        result.HardViolations.Should().Contain(v => v.ViolationCode == "HC-02");
    }

    [Fact]
    public void Validate_WhenNightSequenceCorrect_ShouldNotReportNightSequenceViolation()
    {
        // 夜勤入り -> 夜勤明け -> 休み の正しいシーケンス
        var shiftTypes = CreateTestShiftTypes();
        var staff = new Staff { Id = 1, DisplayName = "山田　太郎" };

        var d1 = new DateOnly(2025, 3, 1);
        var d2 = new DateOnly(2025, 3, 2);
        var d3 = new DateOnly(2025, 3, 3);

        var assignments = new List<ShiftAssignment>
        {
            new() { StaffId = 1, Date = d1, ShiftTypeId = 5 }, // 2階夜勤入り
            new() { StaffId = 1, Date = d2, ShiftTypeId = 9 }, // 夜勤明け
            new() { StaffId = 1, Date = d3, ShiftTypeId = 1 }  // 休み
        };

        var input = new OptimizationInput
        {
            Schedule = new MonthlySchedule { Year = 2025, Month = 3 },
            AllStaff = new List<Staff> { staff },
            ShiftTypes = shiftTypes,
            Settings = new FacilitySettings { MinDaysOff = 0, MaxConsecutiveWorkDays = 10 }
        };

        var result = _validator.Validate(assignments, input);

        result.HardViolations.Should().NotContain(v => v.ViolationCode == "HC-02");
    }

    [Fact]
    public void Validate_WhenConsecutiveWorkExceeds5_ShouldReportHardViolation()
    {
        // 6日連続勤務
        var shiftTypes = CreateTestShiftTypes();
        var staff = new Staff { Id = 1, DisplayName = "山田　太郎" };

        var assignments = new List<ShiftAssignment>();
        for (int d = 1; d <= 6; d++)
        {
            assignments.Add(new ShiftAssignment
            {
                StaffId = 1,
                Date = new DateOnly(2025, 3, d),
                ShiftTypeId = 2 // 日勤
            });
        }

        var input = new OptimizationInput
        {
            Schedule = new MonthlySchedule { Year = 2025, Month = 3 },
            AllStaff = new List<Staff> { staff },
            ShiftTypes = shiftTypes,
            Settings = new FacilitySettings { MinDaysOff = 0, MaxConsecutiveWorkDays = 5 }
        };

        var result = _validator.Validate(assignments, input);

        result.IsValid.Should().BeFalse();
        result.HardViolations.Should().Contain(v => v.ViolationCode == "HC-04");
    }

    [Fact]
    public void Validate_WhenMatsuuraAssignedNightDuty_ShouldReportHardViolation()
    {
        // 松浦洋子（夜勤不可）に夜勤割り当て
        var shiftTypes = CreateTestShiftTypes();
        var matsuura = new Staff { Id = 8, DisplayName = "松浦　洋子" };

        var assignments = new List<ShiftAssignment>
        {
            new() { StaffId = 8, Date = new DateOnly(2025, 3, 1), ShiftTypeId = 5 } // 2階夜勤
        };

        var input = new OptimizationInput
        {
            Schedule = new MonthlySchedule { Year = 2025, Month = 3 },
            AllStaff = new List<Staff> { matsuura },
            ShiftTypes = shiftTypes,
            Settings = new FacilitySettings { MinDaysOff = 0, MaxConsecutiveWorkDays = 5 }
        };

        var result = _validator.Validate(assignments, input);

        result.IsValid.Should().BeFalse();
        result.HardViolations.Should().Contain(v => v.ViolationCode == "HC-07");
    }

    [Fact]
    public void Validate_WhenMiyoshiAssignedFloor2_ShouldReportHardViolation()
    {
        // 三吉孝明（2階不可）に2階早出割り当て
        var shiftTypes = CreateTestShiftTypes();
        var miyoshi = new Staff { Id = 14, DisplayName = "三吉　孝明" };

        var assignments = new List<ShiftAssignment>
        {
            new() { StaffId = 14, Date = new DateOnly(2025, 3, 1), ShiftTypeId = 3 } // 2階早出
        };

        var input = new OptimizationInput
        {
            Schedule = new MonthlySchedule { Year = 2025, Month = 3 },
            AllStaff = new List<Staff> { miyoshi },
            ShiftTypes = shiftTypes,
            Settings = new FacilitySettings { MinDaysOff = 0, MaxConsecutiveWorkDays = 5 }
        };

        var result = _validator.Validate(assignments, input);

        result.IsValid.Should().BeFalse();
        result.HardViolations.Should().Contain(v => v.ViolationCode == "HC-13");
    }
}
