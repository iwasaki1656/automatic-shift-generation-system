using ShiftManagement.Core.Interfaces;
using ShiftManagement.Core.Models;

namespace ShiftManagement.Validation;

public class ShiftValidator : IShiftValidator
{
    public ValidationResult Validate(
        IReadOnlyList<ShiftAssignment> assignments,
        OptimizationInput input)
    {
        var result = new ValidationResult { ValidatedAt = DateTime.Now };

        var staffMap = input.AllStaff.ToDictionary(s => s.Id);
        var shiftTypeMap = input.ShiftTypes.ToDictionary(t => t.Id);
        var shiftCodeMap = input.ShiftTypes.ToDictionary(t => t.Code);

        var daysInMonth = input.Schedule.DaysInMonth;
        var year = input.Schedule.Year;
        var month = input.Schedule.Month;
        var allDates = Enumerable.Range(1, daysInMonth)
            .Select(d => new DateOnly(year, month, d))
            .ToList();

        // 職員ごと・日付ごとのマッピング
        var staffDayMap = assignments
            .GroupBy(a => (a.StaffId, a.Date))
            .ToDictionary(g => g.Key, g => g.First());

        var assignmentsByStaff = assignments
            .GroupBy(a => a.StaffId)
            .ToDictionary(g => g.Key, g => g.OrderBy(a => a.Date).ToList());

        var assignmentsByDate = assignments
            .GroupBy(a => a.Date)
            .ToDictionary(g => g.Key, g => g.ToList());

        // HC-01: 同日複数勤務チェック
        var duplicates = assignments
            .GroupBy(a => (a.StaffId, a.Date))
            .Where(g => g.Count() > 1);
        foreach (var dup in duplicates)
        {
            var staff = staffMap.GetValueOrDefault(dup.Key.StaffId);
            result.HardViolations.Add(new ViolationDetail
            {
                Severity = ViolationSeverity.HardViolation,
                StaffId = dup.Key.StaffId,
                StaffName = staff?.DisplayName ?? "",
                Date = dup.Key.Date,
                ViolationCode = "HC-01",
                Message = $"{staff?.DisplayName}の{dup.Key.Date:M/d}に複数の勤務が割り当てられています。"
            });
        }

        // HC-02: 夜勤シーケンスチェック（入り→明け→休）
        CheckNightSequence(assignmentsByStaff, input, staffMap, shiftTypeMap, result);

        // HC-03: 必要人数の充足チェック
        CheckRequiredCount(assignmentsByDate, input, shiftTypeMap, allDates, result);

        // HC-04: 最大連続勤務日数チェック
        CheckMaxConsecutiveWork(assignmentsByStaff, input, staffMap, shiftTypeMap, result);

        // HC-05: 月間最低休日数チェック
        CheckMinDaysOff(assignmentsByStaff, input, staffMap, shiftTypeMap, daysInMonth, result);

        // HC-06: 日曜日最低1回休日チェック
        CheckSundayOff(assignmentsByStaff, input, staffMap, shiftTypeMap, allDates, result);

        // HC-07〜HC-13: 職員ごとの固定条件・個別条件チェック
        CheckStaffSpecificHardConstraints(assignments, input, staffMap, shiftTypeMap, result);

        // HC-14, HC-15: 月次個人制約のHardチェック
        CheckPersonalHardConstraints(assignments, input, staffMap, shiftTypeMap, result);

        // Soft制約の検証
        CheckSoftConstraints(assignments, input, staffMap, shiftTypeMap, allDates, result);

        return result;
    }

    public ValidationResult ValidateSingleChange(
        IReadOnlyList<ShiftAssignment> assignments,
        ShiftAssignment changedAssignment,
        OptimizationInput input)
    {
        // 変更を反映したリストを構築して全体検証
        var updated = assignments
            .Where(a => !(a.StaffId == changedAssignment.StaffId && a.Date == changedAssignment.Date))
            .ToList();
        updated.Add(changedAssignment);

        return Validate(updated, input);
    }

    #region Hard Constraints Checks

    private void CheckNightSequence(
        Dictionary<int, List<ShiftAssignment>> assignmentsByStaff,
        OptimizationInput input,
        Dictionary<int, Staff> staffMap,
        Dictionary<int, ShiftType> shiftTypeMap,
        ValidationResult result)
    {
        foreach (var (staffId, staffAssignments) in assignmentsByStaff)
        {
            var staff = staffMap.GetValueOrDefault(staffId);
            var sorted = staffAssignments.OrderBy(a => a.Date).ToList();

            for (int i = 0; i < sorted.Count; i++)
            {
                var cur = sorted[i];
                var curShift = shiftTypeMap.GetValueOrDefault(cur.ShiftTypeId);
                if (curShift == null) continue;

                // 夜勤入り（NIGHT_2F or NIGHT_3F）
                if (curShift.ShiftCategory == ShiftCategory.NightIn)
                {
                    // 翌日チェック（月内の場合）
                    if (i + 1 < sorted.Count)
                    {
                        var next = sorted[i + 1];
                        var nextShift = shiftTypeMap.GetValueOrDefault(next.ShiftTypeId);
                        if (nextShift?.ShiftCategory != ShiftCategory.NightOff)
                        {
                            result.HardViolations.Add(new ViolationDetail
                            {
                                Severity = ViolationSeverity.HardViolation,
                                StaffId = staffId,
                                StaffName = staff?.DisplayName ?? "",
                                Date = next.Date,
                                ActualShiftCode = nextShift?.Code,
                                ActualShiftName = nextShift?.DisplayName,
                                ViolationCode = "HC-02",
                                Message = $"{staff?.DisplayName}の夜勤入り翌日（{next.Date:M/d}）が夜勤明け（－）になっていません（現在: {nextShift?.DisplayName}）。"
                            });
                        }
                    }

                    // 翌々日チェック（月内の場合）
                    if (i + 2 < sorted.Count)
                    {
                        var nextNext = sorted[i + 2];
                        var nextNextShift = shiftTypeMap.GetValueOrDefault(nextNext.ShiftTypeId);
                        if (nextNextShift?.IsDayOff != true)
                        {
                            result.HardViolations.Add(new ViolationDetail
                            {
                                Severity = ViolationSeverity.HardViolation,
                                StaffId = staffId,
                                StaffName = staff?.DisplayName ?? "",
                                Date = nextNext.Date,
                                ActualShiftCode = nextNextShift?.Code,
                                ActualShiftName = nextNextShift?.DisplayName,
                                ViolationCode = "HC-02",
                                Message = $"{staff?.DisplayName}の夜勤明け翌日（{nextNext.Date:M/d}）が休み（ヤ/有）になっていません（現在: {nextNextShift?.DisplayName}）。"
                            });
                        }
                    }
                }
                else if (curShift.ShiftCategory == ShiftCategory.NightOff)
                {
                    // 夜勤明けの前日が夜勤入りかチェック（前月繰越がある場合はそれを考慮）
                    if (i > 0)
                    {
                        var prev = sorted[i - 1];
                        var prevShift = shiftTypeMap.GetValueOrDefault(prev.ShiftTypeId);
                        if (prevShift?.ShiftCategory != ShiftCategory.NightIn)
                        {
                            result.HardViolations.Add(new ViolationDetail
                            {
                                Severity = ViolationSeverity.HardViolation,
                                StaffId = staffId,
                                StaffName = staff?.DisplayName ?? "",
                                Date = cur.Date,
                                ActualShiftCode = curShift.Code,
                                ActualShiftName = curShift.DisplayName,
                                ViolationCode = "HC-02",
                                Message = $"{staff?.DisplayName}の{cur.Date:M/d}が夜勤明けですが、前日が夜勤入りではありません。"
                            });
                        }
                    }
                }
            }
        }
    }

    private void CheckRequiredCount(
        Dictionary<DateOnly, List<ShiftAssignment>> assignmentsByDate,
        OptimizationInput input,
        Dictionary<int, ShiftType> shiftTypeMap,
        List<DateOnly> allDates,
        ValidationResult result)
    {
        foreach (var date in allDates)
        {
            var dayAssignments = assignmentsByDate.GetValueOrDefault(date, new List<ShiftAssignment>());
            var countsByShiftId = dayAssignments
                .GroupBy(a => a.ShiftTypeId)
                .ToDictionary(g => g.Key, g => g.Count());

            foreach (var req in input.Requirements.Where(r => r.IsActive))
            {
                var currentCount = countsByShiftId.GetValueOrDefault(req.ShiftTypeId, 0);
                if (currentCount < req.RequiredCount)
                {
                    var shift = shiftTypeMap.GetValueOrDefault(req.ShiftTypeId);
                    result.HardViolations.Add(new ViolationDetail
                    {
                        Severity = ViolationSeverity.HardViolation,
                        Date = date,
                        ActualShiftCode = shift?.Code,
                        ActualShiftName = shift?.DisplayName,
                        ViolationCode = "HC-03",
                        Message = $"{date:M/d}の{shift?.DisplayName}の人数が不足しています（必要: {req.RequiredCount}名, 現在: {currentCount}名）。"
                    });
                }
            }
        }
    }

    private void CheckMaxConsecutiveWork(
        Dictionary<int, List<ShiftAssignment>> assignmentsByStaff,
        OptimizationInput input,
        Dictionary<int, Staff> staffMap,
        Dictionary<int, ShiftType> shiftTypeMap,
        ValidationResult result)
    {
        int maxConsecutive = input.Settings?.MaxConsecutiveWorkDays ?? 5;

        foreach (var (staffId, staffAssignments) in assignmentsByStaff)
        {
            var staff = staffMap.GetValueOrDefault(staffId);
            var sorted = staffAssignments.OrderBy(a => a.Date).ToList();

            int consecutive = 0;
            DateOnly? streakStart = null;

            foreach (var a in sorted)
            {
                var shift = shiftTypeMap.GetValueOrDefault(a.ShiftTypeId);
                if (shift == null) continue;

                if (shift.IsWorkDay && shift.ShiftCategory != ShiftCategory.NightOff)
                {
                    consecutive++;
                    streakStart ??= a.Date;

                    if (consecutive > maxConsecutive)
                    {
                        result.HardViolations.Add(new ViolationDetail
                        {
                            Severity = ViolationSeverity.HardViolation,
                            StaffId = staffId,
                            StaffName = staff?.DisplayName ?? "",
                            Date = a.Date,
                            ViolationCode = "HC-04",
                            Message = $"{staff?.DisplayName}が{consecutive}日連続勤務になっています（上限: {maxConsecutive}日, 開始: {streakStart:M/d}）。"
                        });
                    }
                }
                else
                {
                    consecutive = 0;
                    streakStart = null;
                }
            }
        }
    }

    private void CheckMinDaysOff(
        Dictionary<int, List<ShiftAssignment>> assignmentsByStaff,
        OptimizationInput input,
        Dictionary<int, Staff> staffMap,
        Dictionary<int, ShiftType> shiftTypeMap,
        int daysInMonth,
        ValidationResult result)
    {
        int minDaysOff = input.Settings?.MinDaysOff ?? 9;

        foreach (var (staffId, staffAssignments) in assignmentsByStaff)
        {
            var staff = staffMap.GetValueOrDefault(staffId);
            // パート職員（三吉孝明など）は常勤より休みが多く設定される場合があるが、最低9日は保証
            int daysOff = staffAssignments.Count(a =>
            {
                var s = shiftTypeMap.GetValueOrDefault(a.ShiftTypeId);
                return s != null && s.IsDayOff;
            });

            if (daysOff < minDaysOff)
            {
                result.HardViolations.Add(new ViolationDetail
                {
                    Severity = ViolationSeverity.HardViolation,
                    StaffId = staffId,
                    StaffName = staff?.DisplayName ?? "",
                    ViolationCode = "HC-05",
                    Message = $"{staff?.DisplayName}の月間休日数が不足しています（必要: {minDaysOff}日以上, 現在: {daysOff}日）。"
                });
            }
        }
    }

    private void CheckSundayOff(
        Dictionary<int, List<ShiftAssignment>> assignmentsByStaff,
        OptimizationInput input,
        Dictionary<int, Staff> staffMap,
        Dictionary<int, ShiftType> shiftTypeMap,
        List<DateOnly> allDates,
        ValidationResult result)
    {
        var sundays = allDates.Where(d => d.DayOfWeek == DayOfWeek.Sunday).ToHashSet();
        if (sundays.Count == 0) return;

        foreach (var (staffId, staffAssignments) in assignmentsByStaff)
        {
            var staff = staffMap.GetValueOrDefault(staffId);
            int sundayOffCount = staffAssignments
                .Where(a => sundays.Contains(a.Date))
                .Count(a =>
                {
                    var s = shiftTypeMap.GetValueOrDefault(a.ShiftTypeId);
                    return s != null && s.IsDayOff;
                });

            if (sundayOffCount == 0)
            {
                result.HardViolations.Add(new ViolationDetail
                {
                    Severity = ViolationSeverity.HardViolation,
                    StaffId = staffId,
                    StaffName = staff?.DisplayName ?? "",
                    ViolationCode = "HC-06",
                    Message = $"{staff?.DisplayName}に日曜日休みが1日もありません（最低1回必須）。"
                });
            }
        }
    }

    private void CheckStaffSpecificHardConstraints(
        IReadOnlyList<ShiftAssignment> assignments,
        OptimizationInput input,
        Dictionary<int, Staff> staffMap,
        Dictionary<int, ShiftType> shiftTypeMap,
        ValidationResult result)
    {
        foreach (var a in assignments)
        {
            var staff = staffMap.GetValueOrDefault(a.StaffId);
            var shift = shiftTypeMap.GetValueOrDefault(a.ShiftTypeId);
            if (staff == null || shift == null) continue;

            // 長岡妙子ルール (HC-11)
            if (staff.IsNagaokaTaeko)
            {
                // 2階夜勤, 明け, 休み 以外は不可
                if (shift.Code != "NIGHT_2F" && shift.Code != "NIGHT_OFF" && !shift.IsDayOff)
                {
                    result.HardViolations.Add(new ViolationDetail
                    {
                        Severity = ViolationSeverity.HardViolation,
                        StaffId = staff.Id,
                        StaffName = staff.DisplayName,
                        Date = a.Date,
                        ActualShiftCode = shift.Code,
                        ActualShiftName = shift.DisplayName,
                        ViolationCode = "HC-11",
                        Message = $"長岡妙子は2階夜勤専従です（{a.Date:M/d}に{shift.DisplayName}は不可）。"
                    });
                }
            }

            // 上松伸次ルール (HC-08, HC-12)
            if (staff.IsUematsuShinji)
            {
                if (shift.Floor == 3)
                {
                    result.HardViolations.Add(new ViolationDetail
                    {
                        Severity = ViolationSeverity.HardViolation,
                        StaffId = staff.Id,
                        StaffName = staff.DisplayName,
                        Date = a.Date,
                        ActualShiftCode = shift.Code,
                        ActualShiftName = shift.DisplayName,
                        ViolationCode = "HC-12",
                        Message = $"上松伸次は2階専従です（{a.Date:M/d}の3階勤務{shift.DisplayName}は不可）。"
                    });
                }
            }

            // 三吉孝明ルール (HC-09, HC-13)
            if (staff.IsMiyoshiTakaaki)
            {
                // 3階早出、3階遅出、休み以外不可
                if (shift.Code != "EARLY_3F" && shift.Code != "LATE_3F" && !shift.IsDayOff)
                {
                    result.HardViolations.Add(new ViolationDetail
                    {
                        Severity = ViolationSeverity.HardViolation,
                        StaffId = staff.Id,
                        StaffName = staff.DisplayName,
                        Date = a.Date,
                        ActualShiftCode = shift.Code,
                        ActualShiftName = shift.DisplayName,
                        ViolationCode = "HC-13",
                        Message = $"三吉孝明は3階早出・遅出・休みのみ可能です（{a.Date:M/d}に{shift.DisplayName}は不可）。"
                    });
                }
            }

            // 松浦洋子ルール (HC-07)
            if (staff.IsMatsuuraYoko)
            {
                if (shift.ShiftCategory == ShiftCategory.NightIn)
                {
                    result.HardViolations.Add(new ViolationDetail
                    {
                        Severity = ViolationSeverity.HardViolation,
                        StaffId = staff.Id,
                        StaffName = staff.DisplayName,
                        Date = a.Date,
                        ActualShiftCode = shift.Code,
                        ActualShiftName = shift.DisplayName,
                        ViolationCode = "HC-07",
                        Message = $"松浦洋子は夜勤不可です（{a.Date:M/d}に{shift.DisplayName}は不可）。"
                    });
                }
            }

            // 職員の固定制約（StaffPermanentConstraints）
            foreach (var pc in staff.PermanentConstraints.Where(c => c.IsActive && c.Level == ConstraintLevel.Hard))
            {
                if (pc.Type == ConstraintType.NightUnavailable && shift.ShiftCategory == ShiftCategory.NightIn)
                {
                    result.HardViolations.Add(new ViolationDetail
                    {
                        Severity = ViolationSeverity.HardViolation,
                        StaffId = staff.Id,
                        StaffName = staff.DisplayName,
                        Date = a.Date,
                        ViolationCode = "HC-07",
                        Message = $"{staff.DisplayName}は固定条件により夜勤不可です。"
                    });
                }
                else if (pc.Type == ConstraintType.Floor3Unavailable && shift.Floor == 3)
                {
                    result.HardViolations.Add(new ViolationDetail
                    {
                        Severity = ViolationSeverity.HardViolation,
                        StaffId = staff.Id,
                        StaffName = staff.DisplayName,
                        Date = a.Date,
                        ViolationCode = "HC-08",
                        Message = $"{staff.DisplayName}は3階勤務不可です。"
                    });
                }
                else if (pc.Type == ConstraintType.Floor2Unavailable && shift.Floor == 2)
                {
                    result.HardViolations.Add(new ViolationDetail
                    {
                        Severity = ViolationSeverity.HardViolation,
                        StaffId = staff.Id,
                        StaffName = staff.DisplayName,
                        Date = a.Date,
                        ViolationCode = "HC-09",
                        Message = $"{staff.DisplayName}は2階勤務不可です。"
                    });
                }
            }
        }
    }

    private void CheckPersonalHardConstraints(
        IReadOnlyList<ShiftAssignment> assignments,
        OptimizationInput input,
        Dictionary<int, Staff> staffMap,
        Dictionary<int, ShiftType> shiftTypeMap,
        ValidationResult result)
    {
        var hardConstraints = input.PersonalConstraints
            .Where(c => c.Level == ConstraintLevel.Hard)
            .ToList();

        var assignmentMap = assignments
            .ToDictionary(a => (a.StaffId, a.Date));

        foreach (var c in hardConstraints)
        {
            var staff = staffMap.GetValueOrDefault(c.StaffId);
            if (c.TargetDate.HasValue)
            {
                var key = (c.StaffId, c.TargetDate.Value);
                if (assignmentMap.TryGetValue(key, out var a))
                {
                    var shift = shiftTypeMap.GetValueOrDefault(a.ShiftTypeId);
                    if (shift == null) continue;

                    // HC-14: 勤務不可日の絶対休日
                    if (c.Type == ConstraintType.WorkUnavailable && !shift.IsDayOff)
                    {
                        result.HardViolations.Add(new ViolationDetail
                        {
                            Severity = ViolationSeverity.HardViolation,
                            StaffId = c.StaffId,
                            StaffName = staff?.DisplayName ?? "",
                            Date = c.TargetDate.Value,
                            ActualShiftCode = shift.Code,
                            ActualShiftName = shift.DisplayName,
                            ViolationCode = "HC-14",
                            ConstraintId = c.Id,
                            Message = $"{staff?.DisplayName}の勤務不可日（{c.TargetDate.Value:M/d}）に出勤が割り当てられています。"
                        });
                    }
                    // HC-15: 希望休（Hard）
                    else if (c.Type == ConstraintType.DayOffRequest && !shift.IsDayOff)
                    {
                        result.HardViolations.Add(new ViolationDetail
                        {
                            Severity = ViolationSeverity.HardViolation,
                            StaffId = c.StaffId,
                            StaffName = staff?.DisplayName ?? "",
                            Date = c.TargetDate.Value,
                            ActualShiftCode = shift.Code,
                            ActualShiftName = shift.DisplayName,
                            ViolationCode = "HC-15",
                            ConstraintId = c.Id,
                            Message = $"{staff?.DisplayName}の絶対希望休（{c.TargetDate.Value:M/d}）に休みが割り当てられていません。"
                        });
                    }
                    // 夜勤不可（Hard）
                    else if (c.Type == ConstraintType.NightUnavailable && shift.ShiftCategory == ShiftCategory.NightIn)
                    {
                        result.HardViolations.Add(new ViolationDetail
                        {
                            Severity = ViolationSeverity.HardViolation,
                            StaffId = c.StaffId,
                            StaffName = staff?.DisplayName ?? "",
                            Date = c.TargetDate.Value,
                            ViolationCode = "HC-07",
                            ConstraintId = c.Id,
                            Message = $"{staff?.DisplayName}の夜勤不可日（{c.TargetDate.Value:M/d}）に夜勤が割り当てられています。"
                        });
                    }
                }
            }
        }
    }

    #endregion

    #region Soft Constraints Checks

    private void CheckSoftConstraints(
        IReadOnlyList<ShiftAssignment> assignments,
        OptimizationInput input,
        Dictionary<int, Staff> staffMap,
        Dictionary<int, ShiftType> shiftTypeMap,
        List<DateOnly> allDates,
        ValidationResult result)
    {
        var assignmentMap = assignments.ToDictionary(a => (a.StaffId, a.Date));

        // SC-01: 希望休（Soft）
        // SC-02: 希望勤務（Soft）
        var softPersonalConstraints = input.PersonalConstraints
            .Where(c => c.Level == ConstraintLevel.Soft)
            .ToList();

        foreach (var c in softPersonalConstraints)
        {
            var staff = staffMap.GetValueOrDefault(c.StaffId);
            if (c.TargetDate.HasValue && assignmentMap.TryGetValue((c.StaffId, c.TargetDate.Value), out var a))
            {
                var shift = shiftTypeMap.GetValueOrDefault(a.ShiftTypeId);
                if (shift == null) continue;

                if (c.Type == ConstraintType.DayOffRequest && !shift.IsDayOff)
                {
                    result.SoftViolations.Add(new ViolationDetail
                    {
                        Severity = ViolationSeverity.SoftViolation,
                        StaffId = c.StaffId,
                        StaffName = staff?.DisplayName ?? "",
                        Date = c.TargetDate.Value,
                        ActualShiftCode = shift.Code,
                        ActualShiftName = shift.DisplayName,
                        ViolationCode = "SC-01",
                        ConstraintId = c.Id,
                        PenaltyPoints = 40,
                        Message = $"{staff?.DisplayName}の希望休（{c.TargetDate.Value:M/d}）が満たされていません（出勤: {shift.DisplayName}）。"
                    });
                }
                else if (c.Type == ConstraintType.DayPreference && shift.Code != "DAY")
                {
                    result.SoftViolations.Add(new ViolationDetail
                    {
                        Severity = ViolationSeverity.SoftViolation,
                        StaffId = c.StaffId,
                        StaffName = staff?.DisplayName ?? "",
                        Date = c.TargetDate.Value,
                        ActualShiftCode = shift.Code,
                        ActualShiftName = shift.DisplayName,
                        ViolationCode = "SC-02",
                        ConstraintId = c.Id,
                        PenaltyPoints = 20,
                        Message = $"{staff?.DisplayName}の日勤希望（{c.TargetDate.Value:M/d}）が満たされていません（現在: {shift.DisplayName}）。"
                    });
                }
                else if (c.Type == ConstraintType.NightPreference && shift.ShiftCategory != ShiftCategory.NightIn)
                {
                    result.SoftViolations.Add(new ViolationDetail
                    {
                        Severity = ViolationSeverity.SoftViolation,
                        StaffId = c.StaffId,
                        StaffName = staff?.DisplayName ?? "",
                        Date = c.TargetDate.Value,
                        ActualShiftCode = shift.Code,
                        ActualShiftName = shift.DisplayName,
                        ViolationCode = "SC-02",
                        ConstraintId = c.Id,
                        PenaltyPoints = 20,
                        Message = $"{staff?.DisplayName}の夜勤希望（{c.TargetDate.Value:M/d}）が満たされていません（現在: {shift.DisplayName}）。"
                    });
                }
                else if (c.Type == ConstraintType.NightUnavailable && shift.ShiftCategory == ShiftCategory.NightIn)
                {
                    result.SoftViolations.Add(new ViolationDetail
                    {
                        Severity = ViolationSeverity.SoftViolation,
                        StaffId = c.StaffId,
                        StaffName = staff?.DisplayName ?? "",
                        Date = c.TargetDate.Value,
                        ActualShiftCode = shift.Code,
                        ActualShiftName = shift.DisplayName,
                        ViolationCode = "SC-03",
                        ConstraintId = c.Id,
                        PenaltyPoints = 80,
                        Message = $"{staff?.DisplayName}の夜勤回避要望（{c.TargetDate.Value:M/d}）に夜勤が割り当てられています。"
                    });
                }
            }
        }

        // SC-04: 松浦洋子の日勤優先
        var matsuura = input.AllStaff.FirstOrDefault(s => s.IsMatsuuraYoko);
        if (matsuura != null)
        {
            var matsuuraAssignments = assignments.Where(a => a.StaffId == matsuura.Id);
            foreach (var a in matsuuraAssignments)
            {
                var shift = shiftTypeMap.GetValueOrDefault(a.ShiftTypeId);
                if (shift != null && shift.IsWorkDay && shift.Code != "DAY")
                {
                    result.SoftViolations.Add(new ViolationDetail
                    {
                        Severity = ViolationSeverity.SoftViolation,
                        StaffId = matsuura.Id,
                        StaffName = matsuura.DisplayName,
                        Date = a.Date,
                        ActualShiftCode = shift.Code,
                        ActualShiftName = shift.DisplayName,
                        ViolationCode = "SC-04",
                        PenaltyPoints = 30,
                        Message = $"松浦洋子が出勤日（{a.Date:M/d}）に日勤ではなく{shift.DisplayName}になっています。"
                    });
                }
            }
        }

        // SC-06: 夜勤回数目標
        var nightShifts = assignments
            .Where(a =>
            {
                var s = shiftTypeMap.GetValueOrDefault(a.ShiftTypeId);
                return s?.ShiftCategory == ShiftCategory.NightIn;
            })
            .GroupBy(a => a.StaffId)
            .ToDictionary(g => g.Key, g => g.Count());

        foreach (var target in input.NightDutyTargets)
        {
            var actualCount = nightShifts.GetValueOrDefault(target.StaffId, 0);
            var staff = staffMap.GetValueOrDefault(target.StaffId);

            if (target.TargetCount.HasValue && actualCount != target.TargetCount.Value)
            {
                int diff = Math.Abs(actualCount - target.TargetCount.Value);
                result.SoftViolations.Add(new ViolationDetail
                {
                    Severity = ViolationSeverity.SoftViolation,
                    StaffId = target.StaffId,
                    StaffName = staff?.DisplayName ?? "",
                    ViolationCode = "SC-06",
                    PenaltyPoints = diff * 15,
                    Message = $"{staff?.DisplayName}の夜勤回数（{actualCount}回）が目標（{target.TargetCount.Value}回）と異なります（差: {diff}回）。"
                });
            }
        }

        // SC-09: 三吉孝明の勤務ブロック混在チェック
        var miyoshi = input.AllStaff.FirstOrDefault(s => s.IsMiyoshiTakaaki);
        if (miyoshi != null)
        {
            var miyoshiAssignments = assignments
                .Where(a => a.StaffId == miyoshi.Id)
                .OrderBy(a => a.Date)
                .ToList();

            // 休みで区切られた勤務ブロックごとに早出・遅出が混在していないか確認
            var currentBlock = new List<ShiftAssignment>();
            foreach (var a in miyoshiAssignments)
            {
                var s = shiftTypeMap.GetValueOrDefault(a.ShiftTypeId);
                if (s != null && s.IsWorkDay)
                {
                    currentBlock.Add(a);
                }
                else
                {
                    CheckMiyoshiBlock(currentBlock, miyoshi, shiftTypeMap, result);
                    currentBlock.Clear();
                }
            }
            CheckMiyoshiBlock(currentBlock, miyoshi, shiftTypeMap, result);
        }
    }

    private void CheckMiyoshiBlock(
        List<ShiftAssignment> block,
        Staff miyoshi,
        Dictionary<int, ShiftType> shiftTypeMap,
        ValidationResult result)
    {
        if (block.Count <= 1) return;

        bool hasEarly = false;
        bool hasLate = false;

        foreach (var a in block)
        {
            var s = shiftTypeMap.GetValueOrDefault(a.ShiftTypeId);
            if (s?.Code == "EARLY_3F") hasEarly = true;
            if (s?.Code == "LATE_3F") hasLate = true;
        }

        if (hasEarly && hasLate)
        {
            result.SoftViolations.Add(new ViolationDetail
            {
                Severity = ViolationSeverity.SoftViolation,
                StaffId = miyoshi.Id,
                StaffName = miyoshi.DisplayName,
                Date = block.First().Date,
                ViolationCode = "SC-09",
                PenaltyPoints = 20,
                Message = $"三吉孝明の勤務期間（{block.First().Date:M/d}〜{block.Last().Date:M/d}）に早出と遅出が混在しています。"
            });
        }
    }

    #endregion
}
