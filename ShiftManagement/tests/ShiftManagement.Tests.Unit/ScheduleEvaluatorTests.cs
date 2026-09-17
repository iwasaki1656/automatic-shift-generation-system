using FluentAssertions;
using ShiftManagement.Core.Models;
using ShiftManagement.Validation;
using Xunit;

namespace ShiftManagement.Tests.Unit;

public class ScheduleEvaluatorTests
{
    [Fact]
    public void EvaluateStaff_ShouldAccuratelyCountShiftsAndWorkload()
    {
        var staff = new Staff { Id = 1, DisplayName = "佐藤　花子" };
        var shiftTypes = new List<ShiftType>
        {
            new() { Id = 1, Code = "OFF", ShiftCategory = ShiftCategory.Off, LoadFactor = 0, IsWorkDay = false },
            new() { Id = 2, Code = "DAY", ShiftCategory = ShiftCategory.Day, LoadFactor = 1.0, IsWorkDay = true },
            new() { Id = 3, Code = "NIGHT_2F", ShiftCategory = ShiftCategory.NightIn, Floor = 2, LoadFactor = 3.0, IsWorkDay = true }
        };

        var assignments = new List<ShiftAssignment>
        {
            new() { StaffId = 1, Date = new DateOnly(2025, 3, 1), ShiftTypeId = 2 }, // DAY (+1.0)
            new() { StaffId = 1, Date = new DateOnly(2025, 3, 2), ShiftTypeId = 3 }, // NIGHT_2F (+3.0)
            new() { StaffId = 1, Date = new DateOnly(2025, 3, 3), ShiftTypeId = 1 }  // OFF (+0)
        };

        var input = new OptimizationInput
        {
            Schedule = new MonthlySchedule { Year = 2025, Month = 3 },
            AllStaff = new List<Staff> { staff },
            ShiftTypes = shiftTypes
        };

        var valResult = new ValidationResult();
        var evals = ScheduleEvaluator.EvaluateStaff(assignments, input, valResult);

        evals.Should().HaveCount(1);
        var e = evals[0];
        e.DayCount.Should().Be(1);
        e.NightDutyCount.Should().Be(1);
        e.DayOffCount.Should().Be(1);
        e.WorkloadScore.Should().Be(4.0);
    }
}
