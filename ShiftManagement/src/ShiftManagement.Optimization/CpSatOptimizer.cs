using System.Diagnostics;
using Google.OrTools.Sat;
using ShiftManagement.Core.Interfaces;
using ShiftManagement.Core.Models;
using ShiftManagement.Validation;

namespace ShiftManagement.Optimization;

public class CpSatOptimizer : IShiftOptimizer
{
    private readonly IShiftValidator _validator;

    public CpSatOptimizer(IShiftValidator? validator = null)
    {
        _validator = validator ?? new ShiftValidator();
    }

    public async Task<OptimizationResult> OptimizeAsync(
        OptimizationInput input,
        IProgress<OptimizationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new OptimizationResult();

        return await Task.Run(() =>
        {
            try
            {
                progress?.Report(new OptimizationProgress
                {
                    CurrentCase = 1,
                    TotalCases = 3,
                    Message = "案1（総合評価最大化案）を生成中...",
                    ProgressPercentage = 10
                });

                // 案1: 総合評価最大化
                var case1 = SolveSingleCase(input, caseNumber: 1, caseName: "総合評価最大化案", StrategyType.Balanced, previousCases: null, cancellationToken);
                if (case1 == null)
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = "制約を満たす勤務表を生成できませんでした（解なし）。";
                    result.ConflictAnalysis = ConflictAnalyzer.Analyze(input);
                    stopwatch.Stop();
                    result.TotalTimeSeconds = stopwatch.Elapsed.TotalSeconds;
                    return result;
                }
                case1.IsSelected = true; // デフォルトで案1を選択
                result.Cases.Add(case1);

                if (cancellationToken.IsCancellationRequested) return result;

                progress?.Report(new OptimizationProgress
                {
                    CurrentCase = 2,
                    TotalCases = 3,
                    Message = "案2（夜勤配置最多様化案）を生成中...",
                    ProgressPercentage = 45
                });

                // 案2: 夜勤配置多様化（案1と異なる夜勤パターンを探索）
                var case2 = SolveSingleCase(input, caseNumber: 2, caseName: "夜勤配置最多様化案", StrategyType.NightDiversified, previousCases: result.Cases, cancellationToken);
                if (case2 != null)
                {
                    result.Cases.Add(case2);
                }

                if (cancellationToken.IsCancellationRequested) return result;

                progress?.Report(new OptimizationProgress
                {
                    CurrentCase = 3,
                    TotalCases = 3,
                    Message = "案3（勤務負担均等化案）を生成中...",
                    ProgressPercentage = 80
                });

                // 案3: 勤務負担均等化
                var case3 = SolveSingleCase(input, caseNumber: 3, caseName: "勤務負担均等化案", StrategyType.WorkloadFairness, previousCases: result.Cases, cancellationToken);
                if (case3 != null)
                {
                    result.Cases.Add(case3);
                }

                result.IsSuccess = result.Cases.Count > 0;
                stopwatch.Stop();
                result.TotalTimeSeconds = stopwatch.Elapsed.TotalSeconds;

                progress?.Report(new OptimizationProgress
                {
                    CurrentCase = 3,
                    TotalCases = 3,
                    Message = $"生成完了（{result.Cases.Count}案作成、所要時間: {result.TotalTimeSeconds:F1}秒）",
                    ProgressPercentage = 100
                });

                return result;
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = $"最適化処理中に例外が発生しました: {ex.Message}";
                stopwatch.Stop();
                result.TotalTimeSeconds = stopwatch.Elapsed.TotalSeconds;
                return result;
            }
        }, cancellationToken);
    }

    private enum StrategyType
    {
        Balanced,
        NightDiversified,
        WorkloadFairness
    }

    private ScheduleCase? SolveSingleCase(
        OptimizationInput input,
        int caseNumber,
        string caseName,
        StrategyType strategy,
        List<ScheduleCase>? previousCases,
        CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var model = new CpModel();

        int daysInMonth = input.Schedule.DaysInMonth;
        int year = input.Schedule.Year;
        int month = input.Schedule.Month;
        var allDates = Enumerable.Range(1, daysInMonth)
            .Select(d => new DateOnly(year, month, d))
            .ToList();

        var allStaff = input.AllStaff.Where(s => s.IsActive).ToList();
        var allShifts = input.ShiftTypes.ToList();

        var shiftMap = allShifts.ToDictionary(s => s.Id);
        var shiftByCode = allShifts.ToDictionary(s => s.Code);

        // 変数: x[staffId, date, shiftId] = 1 ならその日にその勤務
        var x = new Dictionary<(int staffId, DateOnly date, int shiftId), BoolVar>();

        foreach (var staff in allStaff)
        {
            foreach (var date in allDates)
            {
                foreach (var shift in allShifts)
                {
                    x[(staff.Id, date, shift.Id)] = model.NewBoolVar($"x_{staff.Id}_{date.Day}_{shift.Code}");
                }
            }
        }

        // ==========================================
        // Hard Constraints (制約)
        // ==========================================

        // HC-01: 1日1勤務
        foreach (var staff in allStaff)
        {
            foreach (var date in allDates)
            {
                var dayVars = allShifts.Select(s => x[(staff.Id, date, s.Id)]).ToList();
                model.AddExactlyOne(dayVars);
            }
        }

        // 勤務シフトIDの取得
        shiftByCode.TryGetValue("NIGHT_2F", out var shiftNight2F);
        shiftByCode.TryGetValue("NIGHT_3F", out var shiftNight3F);
        shiftByCode.TryGetValue("NIGHT_OFF", out var shiftNightOff);
        shiftByCode.TryGetValue("OFF", out var shiftOff);
        shiftByCode.TryGetValue("PAID_OFF", out var shiftPaidOff);
        shiftByCode.TryGetValue("DAY", out var shiftDay);
        shiftByCode.TryGetValue("EARLY_2F", out var shiftEarly2F);
        shiftByCode.TryGetValue("LATE_2F", out var shiftLate2F);
        shiftByCode.TryGetValue("EARLY_3F", out var shiftEarly3F);
        shiftByCode.TryGetValue("LATE_3F", out var shiftLate3F);

        var nightInShifts = new List<ShiftType>();
        if (shiftNight2F != null) nightInShifts.Add(shiftNight2F);
        if (shiftNight3F != null) nightInShifts.Add(shiftNight3F);

        var dayOffShifts = allShifts.Where(s => s.IsDayOff).ToList();

        // HC-02: 夜勤シーケンス（入り -> 明け -> 休み）
        foreach (var staff in allStaff)
        {
            for (int i = 0; i < allDates.Count; i++)
            {
                var date = allDates[i];

                foreach (var nIn in nightInShifts)
                {
                    var nInVar = x[(staff.Id, date, nIn.Id)];

                    // 翌日は夜勤明け（NIGHT_OFF）
                    if (i + 1 < allDates.Count && shiftNightOff != null)
                    {
                        var nextDate = allDates[i + 1];
                        var nextOffVar = x[(staff.Id, nextDate, shiftNightOff.Id)];
                        model.Add(nextOffVar >= nInVar);
                    }

                    // 翌々日は休み（OFF または PAID_OFF）
                    if (i + 2 < allDates.Count)
                    {
                        var nextNextDate = allDates[i + 2];
                        var restVars = dayOffShifts.Select(s => x[(staff.Id, nextNextDate, s.Id)]).ToList();
                        model.Add(LinearExpr.Sum(restVars) >= nInVar);
                    }
                }

                // 夜勤明けの前日は夜勤入りでなければならない
                if (shiftNightOff != null && i > 0)
                {
                    var prevDate = allDates[i - 1];
                    var prevNightVars = nightInShifts.Select(s => x[(staff.Id, prevDate, s.Id)]).ToList();
                    var curNightOffVar = x[(staff.Id, date, shiftNightOff.Id)];
                    model.Add(LinearExpr.Sum(prevNightVars) >= curNightOffVar);
                }
                else if (shiftNightOff != null && i == 0)
                {
                    // 1日の夜勤明けは、前月末の繰越情報がない限り禁止
                    bool hadPreviousNightIn = input.PreviousMonthTrailingAssignments?.Any(a => a.StaffId == staff.Id && nightInShifts.Any(n => n.Id == a.ShiftTypeId)) == true;
                    if (!hadPreviousNightIn)
                    {
                        model.Add(x[(staff.Id, date, shiftNightOff.Id)] == 0);
                    }
                }
            }
        }

        // HC-03: 毎日の必要人数を満たす
        foreach (var date in allDates)
        {
            foreach (var req in input.Requirements.Where(r => r.IsActive))
            {
                var staffWorkingThisShift = allStaff.Select(staff => x[(staff.Id, date, req.ShiftTypeId)]).ToList();
                model.Add(LinearExpr.Sum(staffWorkingThisShift) >= req.RequiredCount);
            }
        }

        // HC-04: 最大連続勤務日数 (<= 5日)
        int maxConsecutive = input.Settings?.MaxConsecutiveWorkDays ?? 5;
        // 通常勤務（日勤、早出、遅出、夜勤入りなど、休み・夜勤明け以外）
        var workShifts = allShifts.Where(s => s.IsWorkDay && s.ShiftCategory != ShiftCategory.NightOff).ToList();
        foreach (var staff in allStaff)
        {
            for (int i = 0; i <= allDates.Count - (maxConsecutive + 1); i++)
            {
                var windowVars = new List<BoolVar>();
                for (int w = 0; w <= maxConsecutive; w++)
                {
                    var d = allDates[i + w];
                    foreach (var ws in workShifts)
                    {
                        windowVars.Add(x[(staff.Id, d, ws.Id)]);
                    }
                }
                // 連続する(maxConsecutive + 1)日間の勤務合計は maxConsecutive 以下
                model.Add(LinearExpr.Sum(windowVars) <= maxConsecutive);
            }
        }

        // HC-05: 月間最低休日数 (>= 9日)
        int minDaysOff = input.Settings?.MinDaysOff ?? 9;
        foreach (var staff in allStaff)
        {
            var offVars = new List<BoolVar>();
            foreach (var date in allDates)
            {
                foreach (var offShift in dayOffShifts)
                {
                    offVars.Add(x[(staff.Id, date, offShift.Id)]);
                }
            }
            model.Add(LinearExpr.Sum(offVars) >= minDaysOff);
        }

        // HC-06: 日曜日最低1回休日
        var sundays = allDates.Where(d => d.DayOfWeek == DayOfWeek.Sunday).ToList();
        if (sundays.Count > 0)
        {
            foreach (var staff in allStaff)
            {
                var sundayOffVars = new List<BoolVar>();
                foreach (var sun in sundays)
                {
                    foreach (var offShift in dayOffShifts)
                    {
                        sundayOffVars.Add(x[(staff.Id, sun, offShift.Id)]);
                    }
                }
                model.Add(LinearExpr.Sum(sundayOffVars) >= 1);
            }
        }

        // HC-07〜HC-13: 職員固有の制約
        foreach (var staff in allStaff)
        {
            // 松浦洋子: 夜勤不可
            if (staff.IsMatsuuraYoko)
            {
                foreach (var date in allDates)
                {
                    foreach (var nIn in nightInShifts)
                    {
                        model.Add(x[(staff.Id, date, nIn.Id)] == 0);
                    }
                }
            }

            // 上松伸次: 3階勤務禁止 (EARLY_3F, LATE_3F, NIGHT_3F)
            if (staff.IsUematsuShinji)
            {
                var floor3Shifts = allShifts.Where(s => s.Floor == 3).ToList();
                foreach (var date in allDates)
                {
                    foreach (var f3 in floor3Shifts)
                    {
                        model.Add(x[(staff.Id, date, f3.Id)] == 0);
                    }
                }
            }

            // 三吉孝明: 3階早出、3階遅出、休みのみ
            if (staff.IsMiyoshiTakaaki)
            {
                var allowedShifts = allShifts.Where(s => s.Code is "EARLY_3F" or "LATE_3F" or "OFF" or "PAID_OFF").Select(s => s.Id).ToHashSet();
                foreach (var date in allDates)
                {
                    foreach (var shift in allShifts)
                    {
                        if (!allowedShifts.Contains(shift.Id))
                        {
                            model.Add(x[(staff.Id, date, shift.Id)] == 0);
                        }
                    }
                }
            }

            // 長岡妙子: 2階夜勤専従（希望日のみ2階夜勤、他は休み）
            if (staff.IsNagaokaTaeko)
            {
                var nagaokaAllowedShifts = allShifts.Where(s => s.Code is "NIGHT_2F" or "NIGHT_OFF" or "OFF" or "PAID_OFF").Select(s => s.Id).ToHashSet();
                foreach (var date in allDates)
                {
                    foreach (var shift in allShifts)
                    {
                        if (!nagaokaAllowedShifts.Contains(shift.Id))
                        {
                            model.Add(x[(staff.Id, date, shift.Id)] == 0);
                        }
                    }
                }
            }

            // 固定制約
            foreach (var pc in staff.PermanentConstraints.Where(c => c.IsActive && c.Level == ConstraintLevel.Hard))
            {
                if (pc.Type == ConstraintType.NightUnavailable)
                {
                    foreach (var date in allDates)
                    {
                        foreach (var nIn in nightInShifts)
                            model.Add(x[(staff.Id, date, nIn.Id)] == 0);
                    }
                }
                else if (pc.Type == ConstraintType.Floor3Unavailable)
                {
                    foreach (var date in allDates)
                    {
                        foreach (var f3 in allShifts.Where(s => s.Floor == 3))
                            model.Add(x[(staff.Id, date, f3.Id)] == 0);
                    }
                }
                else if (pc.Type == ConstraintType.Floor2Unavailable)
                {
                    foreach (var date in allDates)
                    {
                        foreach (var f2 in allShifts.Where(s => s.Floor == 2))
                            model.Add(x[(staff.Id, date, f2.Id)] == 0);
                    }
                }
            }
        }

        // HC-14, HC-15: 月次個人Hard制約
        foreach (var c in input.PersonalConstraints.Where(c => c.Level == ConstraintLevel.Hard && c.TargetDate.HasValue))
        {
            var date = c.TargetDate!.Value;
            if (c.Type == ConstraintType.WorkUnavailable || c.Type == ConstraintType.DayOffRequest)
            {
                // 休み必須
                var offVars = dayOffShifts.Select(s => x[(c.StaffId, date, s.Id)]).ToList();
                model.Add(LinearExpr.Sum(offVars) == 1);
            }
            else if (c.Type == ConstraintType.NightUnavailable)
            {
                foreach (var nIn in nightInShifts)
                {
                    model.Add(x[(c.StaffId, date, nIn.Id)] == 0);
                }
            }
        }

        // ==========================================
        // Soft Constraints (ペナルティ最小化)
        // ==========================================
        var penaltyExprs = new List<LinearExpr>();

        // SC-01 & SC-02: 個人の希望（Soft）
        foreach (var c in input.PersonalConstraints.Where(c => c.Level == ConstraintLevel.Soft && c.TargetDate.HasValue))
        {
            var date = c.TargetDate!.Value;
            int weight = c.Priority * 10;

            if (c.Type == ConstraintType.DayOffRequest)
            {
                // 休みでなければペナルティ
                var isWorkVars = allShifts.Where(s => !s.IsDayOff).Select(s => x[(c.StaffId, date, s.Id)]).ToList();
                penaltyExprs.Add(LinearExpr.Sum(isWorkVars) * weight);
            }
            else if (c.Type == ConstraintType.DayPreference && shiftDay != null)
            {
                // 日勤でない場合にペナルティ
                var notDayVars = allShifts.Where(s => s.Id != shiftDay.Id).Select(s => x[(c.StaffId, date, s.Id)]).ToList();
                penaltyExprs.Add(LinearExpr.Sum(notDayVars) * weight);
            }
            else if (c.Type == ConstraintType.NightPreference)
            {
                // 夜勤でない場合にペナルティ
                var notNightVars = allShifts.Where(s => !nightInShifts.Any(n => n.Id == s.Id)).Select(s => x[(c.StaffId, date, s.Id)]).ToList();
                penaltyExprs.Add(LinearExpr.Sum(notNightVars) * weight);
            }
            else if (c.Type == ConstraintType.NightUnavailable)
            {
                // 夜勤の場合にペナルティ
                var nightVars = nightInShifts.Select(n => x[(c.StaffId, date, n.Id)]).ToList();
                penaltyExprs.Add(LinearExpr.Sum(nightVars) * 80);
            }
        }

        // SC-04: 松浦洋子の日勤優先
        var matsuura = allStaff.FirstOrDefault(s => s.IsMatsuuraYoko);
        if (matsuura != null && shiftDay != null)
        {
            foreach (var date in allDates)
            {
                // 出勤日（休み以外）で日勤でない場合にペナルティ
                var otherWorkShifts = workShifts.Where(s => s.Id != shiftDay.Id).Select(s => x[(matsuura.Id, date, s.Id)]).ToList();
                penaltyExprs.Add(LinearExpr.Sum(otherWorkShifts) * 30);
            }
        }

        // SC-06: 夜勤回数目標
        foreach (var target in input.NightDutyTargets)
        {
            if (target.TargetCount.HasValue)
            {
                var staffNightVars = new List<BoolVar>();
                foreach (var date in allDates)
                {
                    foreach (var nIn in nightInShifts)
                        staffNightVars.Add(x[(target.StaffId, date, nIn.Id)]);
                }

                var diffVar = model.NewIntVar(-31, 31, $"night_diff_{target.StaffId}");
                model.Add(diffVar == LinearExpr.Sum(staffNightVars) - target.TargetCount.Value);

                var absDiffVar = model.NewIntVar(0, 31, $"night_abs_diff_{target.StaffId}");
                model.AddAbsEquality(absDiffVar, diffVar);

                penaltyExprs.Add(absDiffVar * 15);
            }
        }

        // 多様化・別戦略用の目的関数調整
        if (strategy == StrategyType.NightDiversified && previousCases?.Count > 0)
        {
            // 案1の夜勤と同一の夜勤割り当てにペナルティを課して別の解へ誘導
            var case1 = previousCases[0];
            var case1Nights = case1.Assignments
                .Where(a => nightInShifts.Any(n => n.Id == a.ShiftTypeId))
                .ToList();

            foreach (var c1n in case1Nights)
            {
                if (x.TryGetValue((c1n.StaffId, c1n.Date, c1n.ShiftTypeId), out var v))
                {
                    penaltyExprs.Add(v * 25);
                }
            }
        }
        else if (strategy == StrategyType.WorkloadFairness)
        {
            // 夜勤可能職員間で夜勤回数の偏りをさらに抑える
            var nightEligibleStaff = allStaff
                .Where(s => !s.IsMatsuuraYoko && !s.IsMiyoshiTakaaki && s.EmploymentType?.Code == "FULL_TIME")
                .ToList();

            if (nightEligibleStaff.Count > 1)
            {
                // 各自の夜勤回数変数
                var staffNightSums = new List<IntVar>();
                foreach (var s in nightEligibleStaff)
                {
                    var sVars = new List<BoolVar>();
                    foreach (var d in allDates)
                    {
                        foreach (var nIn in nightInShifts)
                            sVars.Add(x[(s.Id, d, nIn.Id)]);
                    }
                    var sSum = model.NewIntVar(0, 15, $"night_count_{s.Id}");
                    model.Add(sSum == LinearExpr.Sum(sVars));
                    staffNightSums.Add(sSum);
                }

                // ペア間の夜勤回数差を最小化
                for (int i = 0; i < staffNightSums.Count; i++)
                {
                    for (int j = i + 1; j < staffNightSums.Count; j++)
                    {
                        var pairDiff = model.NewIntVar(-15, 15, $"pair_diff_{i}_{j}");
                        model.Add(pairDiff == staffNightSums[i] - staffNightSums[j]);
                        var absPairDiff = model.NewIntVar(0, 15, $"abs_pair_diff_{i}_{j}");
                        model.AddAbsEquality(absPairDiff, pairDiff);
                        penaltyExprs.Add(absPairDiff * 20);
                    }
                }
            }
        }

        // 目的関数設定
        if (penaltyExprs.Count > 0)
        {
            model.Minimize(LinearExpr.Sum(penaltyExprs));
        }

        // ==========================================
        // ソルバー実行
        // ==========================================
        var solver = new CpSolver();
        solver.StringParameters = strategy switch
        {
            StrategyType.Balanced => "max_time_in_seconds:30.0,num_search_workers:8",
            StrategyType.NightDiversified => "max_time_in_seconds:30.0,num_search_workers:8,random_seed:42",
            StrategyType.WorkloadFairness => "max_time_in_seconds:30.0,num_search_workers:8,random_seed:123",
            _ => "max_time_in_seconds:30.0"
        };

        var status = solver.Solve(model);
        sw.Stop();

        if (status is not (CpSolverStatus.Optimal or CpSolverStatus.Feasible))
        {
            return null;
        }

        // 割り当ての構築
        var assignments = new List<ShiftAssignment>();
        foreach (var staff in allStaff)
        {
            foreach (var date in allDates)
            {
                foreach (var shift in allShifts)
                {
                    if (solver.Value(x[(staff.Id, date, shift.Id)]) == 1)
                    {
                        assignments.Add(new ShiftAssignment
                        {
                            StaffId = staff.Id,
                            Staff = staff,
                            Date = date,
                            ShiftTypeId = shift.Id,
                            ShiftType = shift
                        });
                        break;
                    }
                }
            }
        }

        // 独立バリデーターによる評価
        var validation = _validator.Validate(assignments, input);
        var staffEvaluations = ScheduleEvaluator.EvaluateStaff(assignments, input, validation);
        var metrics = ScheduleEvaluator.CalculateMetrics(staffEvaluations, validation, input);

        var scheduleCase = new ScheduleCase
        {
            CaseNumber = caseNumber,
            CaseName = caseName,
            TotalScore = metrics.TotalScore,
            HardViolationCount = validation.HardViolations.Count,
            SoftViolationCount = validation.SoftViolations.Count,
            PersonalWishAchievementRate = metrics.WishAchievementRate,
            NightDutyVariance = metrics.NightDutyVariance,
            WorkloadVariance = metrics.WorkloadVariance,
            SolverTimeSeconds = sw.Elapsed.TotalSeconds,
            CreatedAt = DateTime.UtcNow,
            Assignments = assignments,
            StaffEvaluations = staffEvaluations,
            ValidationResult = new ValidationResultData
            {
                IsValid = validation.IsValid,
                Summary = validation.Summary,
                ValidatedAt = validation.ValidatedAt
            }
        };

        return scheduleCase;
    }
}
