using ShiftManagement.Core.Models;

namespace ShiftManagement.Optimization;

public static class ConflictAnalyzer
{
    public static ConflictAnalysis Analyze(OptimizationInput input)
    {
        var analysis = new ConflictAnalysis();
        var daysInMonth = input.Schedule.DaysInMonth;
        var year = input.Schedule.Year;
        var month = input.Schedule.Month;
        var allDates = Enumerable.Range(1, daysInMonth)
            .Select(d => new DateOnly(year, month, d))
            .ToList();

        var shiftTypeMap = input.ShiftTypes.ToDictionary(t => t.Id);
        var staffMap = input.AllStaff.ToDictionary(s => s.Id);

        // 各日の勤務不可者・希望休者を調査
        var hardUnavailableByDate = input.PersonalConstraints
            .Where(c => c.Level == ConstraintLevel.Hard && c.TargetDate.HasValue &&
                        (c.Type == ConstraintType.WorkUnavailable || c.Type == ConstraintType.DayOffRequest))
            .GroupBy(c => c.TargetDate!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        // 各日の必要人数合計
        int totalDailyRequired = input.Requirements.Where(r => r.IsActive).Sum(r => r.RequiredCount);

        foreach (var date in allDates)
        {
            var hardUnavail = hardUnavailableByDate.GetValueOrDefault(date, new List<PersonalConstraint>());
            int availableStaffCount = input.AllStaff.Count - hardUnavail.Count;

            if (availableStaffCount < totalDailyRequired)
            {
                analysis.ConflictDescriptions.Add(
                    $"{date:M/d}の人員不足: 必要{totalDailyRequired}名に対し、出勤可能者が{availableStaffCount}名しかいません（不可/希望休: {hardUnavail.Count}名）。");

                foreach (var c in hardUnavail)
                {
                    var staff = staffMap.GetValueOrDefault(c.StaffId);
                    analysis.RelaxationSuggestions.Add(new RelaxationSuggestion
                    {
                        StaffName = staff?.DisplayName ?? "",
                        Date = date,
                        ConstraintDescription = c.Type == ConstraintType.WorkUnavailable ? "勤務不可(Hard)" : "希望休(Hard)",
                        RelaxationAction = "Hard制約からSoft制約に緩和する、または日を変更する",
                        ExpectedBenefit = $"{date:M/d}の必要人員を充足できる可能性があります"
                    });
                }
            }

            // 夜勤専従（長岡妙子）と夜勤枠の適合チェック
            var nagaoka = input.AllStaff.FirstOrDefault(s => s.IsNagaokaTaeko);
            if (nagaoka != null)
            {
                var nagaokaWishes = input.PersonalConstraints
                    .Where(c => c.StaffId == nagaoka.Id && c.TargetDate == date &&
                                (c.Type == ConstraintType.NightPreference || c.Type == ConstraintType.NagaokaSpecialRule))
                    .ToList();
            }
        }

        // 月間休日数の合計と必要勤務枠の整合性チェック
        int totalWorkSlotsNeeded = totalDailyRequired * daysInMonth;
        int totalStaffCapacity = input.AllStaff.Count * (daysInMonth - input.Settings.MinDaysOff);

        if (totalStaffCapacity < totalWorkSlotsNeeded)
        {
            analysis.ConflictDescriptions.Add(
                $"月間の総枠不足: 全職員が最低{input.Settings.MinDaysOff}日休んだ場合の最大総勤務日数は{totalStaffCapacity}日ですが、月間の必要枠は{totalWorkSlotsNeeded}日です（{totalWorkSlotsNeeded - totalStaffCapacity}日不足）。");

            analysis.RelaxationSuggestions.Add(new RelaxationSuggestion
            {
                StaffName = "施設全体",
                Date = null,
                ConstraintDescription = "月間最低休日数（現在: " + input.Settings.MinDaysOff + "日）",
                RelaxationAction = "最低休日数を8日に変更する、または非常勤職員を増やす",
                ExpectedBenefit = "月間の総必要人枠を満たすことができます"
            });
        }

        if (analysis.ConflictDescriptions.Count == 0)
        {
            analysis.ConflictDescriptions.Add("複雑な制約（夜勤シーケンス、連続勤務制限、職員別担当可能業務）の組み合わせにより実行可能解が見つかりませんでした。");
            analysis.RelaxationSuggestions.Add(new RelaxationSuggestion
            {
                StaffName = "希望休・不可日の多い職員",
                Date = null,
                ConstraintDescription = "個人希望（Hard）",
                RelaxationAction = "一部のHard制約をSoft制約に変更して再実行してください",
                ExpectedBenefit = "解の探索空間が広がり、実行可能解が得られやすくなります"
            });
        }

        return analysis;
    }
}
