using ShiftManagement.Core.Interfaces;
using ShiftManagement.Core.Models;

namespace ShiftManagement.Validation;

public static class ScheduleEvaluator
{
    public static List<StaffEvaluation> EvaluateStaff(
        IReadOnlyList<ShiftAssignment> assignments,
        OptimizationInput input,
        ValidationResult validationResult)
    {
        var shiftTypeMap = input.ShiftTypes.ToDictionary(t => t.Id);
        var staffEvaluations = new List<StaffEvaluation>();

        var softViolationsByStaff = validationResult.SoftViolations
            .Where(v => v.StaffId.HasValue)
            .GroupBy(v => v.StaffId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        var assignmentsByStaff = assignments
            .GroupBy(a => a.StaffId)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var staff in input.AllStaff)
        {
            var staffAssignments = assignmentsByStaff.GetValueOrDefault(staff.Id, new List<ShiftAssignment>());
            var eval = new StaffEvaluation
            {
                StaffId = staff.Id,
                Staff = staff
            };

            double workloadTotal = 0;

            foreach (var a in staffAssignments)
            {
                var shift = shiftTypeMap.GetValueOrDefault(a.ShiftTypeId);
                if (shift == null) continue;

                if (shift.IsWorkDay && shift.ShiftCategory != ShiftCategory.NightOff)
                {
                    eval.WorkDayCount++;
                }

                if (shift.IsDayOff)
                {
                    eval.DayOffCount++;
                }

                if (shift.ShiftCategory == ShiftCategory.NightIn)
                {
                    eval.NightDutyCount++;
                }

                if (shift.ShiftCategory == ShiftCategory.Early)
                {
                    eval.EarlyCount++;
                }

                if (shift.ShiftCategory == ShiftCategory.Late)
                {
                    eval.LateCount++;
                }

                if (shift.ShiftCategory == ShiftCategory.Day)
                {
                    eval.DayCount++;
                }

                if (shift.Floor == 2)
                {
                    eval.Floor2Count++;
                }
                else if (shift.Floor == 3)
                {
                    eval.Floor3Count++;
                }

                workloadTotal += shift.LoadFactor;
            }

            eval.WorkloadScore = workloadTotal;

            // 個人希望の達成・未達成
            var staffWishes = input.PersonalConstraints
                .Where(c => c.StaffId == staff.Id && c.Level == ConstraintLevel.Soft)
                .ToList();

            var violations = softViolationsByStaff.GetValueOrDefault(staff.Id, new List<ViolationDetail>());
            eval.SoftViolationCount = violations.Count;
            eval.SoftAchievedCount = Math.Max(0, staffWishes.Count - violations.Count);

            staffEvaluations.Add(eval);
        }

        return staffEvaluations;
    }

    public static (double TotalScore, double WishAchievementRate, double NightDutyVariance, double WorkloadVariance) CalculateMetrics(
        List<StaffEvaluation> staffEvals,
        ValidationResult validationResult,
        OptimizationInput input)
    {
        // 個人希望達成率
        int totalWishes = input.PersonalConstraints.Count(c => c.Level == ConstraintLevel.Soft);
        int unfulfilled = validationResult.SoftViolations.Count(v => v.ViolationCode is "SC-01" or "SC-02" or "SC-03");
        double wishRate = totalWishes > 0
            ? Math.Max(0, 100.0 * (totalWishes - unfulfilled) / totalWishes)
            : 100.0;

        // 夜勤対象者（夜勤可能者のみ）の夜勤回数の分散
        var eligibleForNight = staffEvals
            .Where(e => !e.Staff?.IsMatsuuraYoko == true && !e.Staff?.IsMiyoshiTakaaki == true)
            .Select(e => (double)e.NightDutyCount)
            .ToList();

        double nightVariance = CalculateVariance(eligibleForNight);

        // 勤務負担の分散（常勤のみ）
        var fullTimeLoads = staffEvals
            .Where(e => e.Staff?.EmploymentType?.Code == "FULL_TIME")
            .Select(e => e.WorkloadScore)
            .ToList();

        double loadVariance = CalculateVariance(fullTimeLoads);

        // 総合スコア (100点満点からペナルティ減点)
        double penalty = validationResult.TotalPenalty;
        double score = Math.Max(0, 100.0 - (penalty * 0.1));

        return (Math.Round(score, 1), Math.Round(wishRate, 1), Math.Round(nightVariance, 2), Math.Round(loadVariance, 2));
    }

    private static double CalculateVariance(List<double> values)
    {
        if (values.Count <= 1) return 0;
        double avg = values.Average();
        double sumSquares = values.Sum(v => Math.Pow(v - avg, 2));
        return sumSquares / values.Count;
    }
}
