using ClosedXML.Excel;
using ShiftManagement.Core.Interfaces;
using ShiftManagement.Core.Models;

namespace ShiftManagement.Excel;

public class ExcelExporter
{
    private readonly string? _templatePath;

    public ExcelExporter(string? templatePath = null)
    {
        _templatePath = templatePath;
    }

    public async Task ExportScheduleAsync(
        ScheduleCase scheduleCase,
        OptimizationInput input,
        string outputPath,
        bool highlightWishes = true)
    {
        await Task.Run(() =>
        {
            XLWorkbook workbook;

            if (!string.IsNullOrEmpty(_templatePath) && File.Exists(_templatePath))
            {
                workbook = new XLWorkbook(_templatePath);
            }
            else
            {
                workbook = new XLWorkbook();
                workbook.Worksheets.Add("勤務表");
            }

            var scheduleSheet = workbook.Worksheet("勤務表") ?? workbook.Worksheets.First();
            PopulateScheduleSheet(scheduleSheet, scheduleCase, input, highlightWishes);

            // 追加シート: 個人別評価
            var evalSheet = workbook.Worksheets.Add("個人別評価");
            PopulateEvaluationSheet(evalSheet, scheduleCase, input);

            // 追加シート: 条件一覧
            var constraintSheet = workbook.Worksheets.Add("条件一覧");
            PopulateConstraintSheet(constraintSheet, scheduleCase, input);

            // 追加シート: 日別集計
            var summarySheet = workbook.Worksheets.Add("日別集計");
            PopulateDailySummarySheet(summarySheet, scheduleCase, input);

            workbook.SaveAs(outputPath);
        });
    }

    private void PopulateScheduleSheet(
        IXLWorksheet ws,
        ScheduleCase scheduleCase,
        OptimizationInput input,
        bool highlightWishes)
    {
        int year = input.Schedule.Year;
        int month = input.Schedule.Month;
        int daysInMonth = input.Schedule.DaysInMonth;

        // タイトル
        ws.Cell("A1").Value = $"{year}年";
        ws.Cell("D1").Value = input.Settings.FacilityName;
        ws.Cell("A2").Value = $"{month}月勤務表";

        // 日付・曜日ヘッダー
        // 列D=1日, 列AH=31日
        for (int day = 1; day <= daysInMonth; day++)
        {
            int col = 3 + day; // D=4
            var date = new DateOnly(year, month, day);

            ws.Cell(5, col).Value = day;
            var dayOfWeekStr = date.DayOfWeek switch
            {
                DayOfWeek.Sunday => "日",
                DayOfWeek.Monday => "月",
                DayOfWeek.Tuesday => "火",
                DayOfWeek.Wednesday => "水",
                DayOfWeek.Thursday => "木",
                DayOfWeek.Friday => "金",
                DayOfWeek.Saturday => "土",
                _ => ""
            };
            ws.Cell(6, col).Value = dayOfWeekStr;

            // 曜日の色分け
            if (date.DayOfWeek == DayOfWeek.Sunday)
            {
                ws.Cell(6, col).Style.Font.FontColor = XLColor.Red;
                ws.Cell(6, col).Style.Fill.BackgroundColor = XLColor.FromHtml("#FCE4EC");
            }
            else if (date.DayOfWeek == DayOfWeek.Saturday)
            {
                ws.Cell(6, col).Style.Font.FontColor = XLColor.Blue;
                ws.Cell(6, col).Style.Fill.BackgroundColor = XLColor.FromHtml("#E3F2FD");
            }
        }

        // 31日未満の月の場合、余分な日付列をクリア
        for (int day = daysInMonth + 1; day <= 31; day++)
        {
            int col = 3 + day;
            ws.Cell(5, col).Value = "";
            ws.Cell(6, col).Value = "";
        }

        // 職員ごとの勤務割り当て
        var assignmentMap = scheduleCase.Assignments
            .ToDictionary(a => (a.StaffId, a.Date));

        var shiftTypeMap = input.ShiftTypes.ToDictionary(t => t.Id);

        // 職員リスト（看護職→介護職順）
        var staffList = input.AllStaff
            .Where(s => s.IsActive)
            .OrderBy(s => s.DisplayOrder)
            .ToList();

        // 看護職と介護職の開始行設定
        // サンプルレイアウト準拠: 行8〜10 看護職、行12〜 介護職
        int currentNursingRow = 8;
        int currentCareRow = 12;

        foreach (var staff in staffList)
        {
            int row;
            if (staff.JobType?.Code == "NURSING")
            {
                row = currentNursingRow++;
            }
            else
            {
                row = currentCareRow++;
            }

            ws.Cell(row, 1).Value = staff.JobType?.DisplayName ?? "";
            ws.Cell(row, 2).Value = staff.EmploymentType?.DisplayName ?? "";
            ws.Cell(row, 3).Value = staff.DisplayName;

            int offCount = 0;
            int paidOffCount = 0;
            int nightCount = 0;
            int dayCount = 0;
            int early2Count = 0;
            int late2Count = 0;
            int early3Count = 0;
            int late3Count = 0;

            for (int day = 1; day <= daysInMonth; day++)
            {
                int col = 3 + day;
                var date = new DateOnly(year, month, day);

                if (assignmentMap.TryGetValue((staff.Id, date), out var assignment))
                {
                    var shift = shiftTypeMap.GetValueOrDefault(assignment.ShiftTypeId);
                    if (shift != null)
                    {
                        var cell = ws.Cell(row, col);
                        cell.Value = shift.DisplaySymbol;
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                        // フォントカラー・背景色
                        if (!string.IsNullOrEmpty(shift.ExcelFontColor))
                        {
                            try { cell.Style.Font.FontColor = XLColor.FromHtml(shift.ExcelFontColor); } catch { }
                        }
                        if (!string.IsNullOrEmpty(shift.ExcelBackgroundColor))
                        {
                            try { cell.Style.Fill.BackgroundColor = XLColor.FromHtml(shift.ExcelBackgroundColor); } catch { }
                        }

                        // 集計加算
                        if (shift.Code == "OFF") offCount++;
                        else if (shift.Code == "PAID_OFF") paidOffCount++;
                        else if (shift.ShiftCategory == ShiftCategory.NightIn) nightCount++;
                        else if (shift.Code == "DAY") dayCount++;
                        else if (shift.Code == "EARLY_2F") early2Count++;
                        else if (shift.Code == "LATE_2F") late2Count++;
                        else if (shift.Code == "EARLY_3F") early3Count++;
                        else if (shift.Code == "LATE_3F") late3Count++;
                    }
                }
            }

            // 集計列の書き込み (AI〜AV)
            ws.Cell(row, 35).Value = offCount;       // AI: 休み
            ws.Cell(row, 36).Value = paidOffCount;   // AJ: 有給
            ws.Cell(row, 37).Value = nightCount;     // AK: 夜勤
            ws.Cell(row, 38).Value = staff.Qualification?.DisplayName ?? ""; // AL: 資格
            ws.Cell(row, 40).Value = dayCount;       // AN: 日勤数
            ws.Cell(row, 41).Value = nightCount;     // AO: ヤ（夜勤計）
            ws.Cell(row, 44).Value = paidOffCount;   // AR: 有
            ws.Cell(row, 45).Value = early2Count;    // AS: 2早
            ws.Cell(row, 46).Value = late2Count;     // AT: 2オ
            ws.Cell(row, 47).Value = early3Count;    // AU: 3早
            ws.Cell(row, 48).Value = late3Count;     // AV: 3オ
        }
    }

    private void PopulateEvaluationSheet(
        IXLWorksheet ws,
        ScheduleCase scheduleCase,
        OptimizationInput input)
    {
        ws.Cell("A1").Value = "勤務表 評価・個人統計シート";
        ws.Cell("A1").Style.Font.Bold = true;
        ws.Cell("A1").Style.Font.FontSize = 14;

        ws.Cell("A2").Value = $"案: {scheduleCase.CaseName} | 総合スコア: {scheduleCase.TotalScore:F1}点 | 希望達成率: {scheduleCase.PersonalWishAchievementRate:F1}%";

        // テーブルヘッダー
        string[] headers = {
            "職種", "勤務形態", "氏名", "資格", "勤務日数", "休日数",
            "夜勤回数", "早出回数", "遅出回数", "日勤回数", "2階勤務", "3階勤務",
            "勤務負担スコア", "希望達成数", "未達成数"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(4, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1F497D");
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        int row = 5;
        foreach (var eval in scheduleCase.StaffEvaluations.OrderBy(e => e.Staff?.DisplayOrder ?? 0))
        {
            var staff = eval.Staff;
            ws.Cell(row, 1).Value = staff?.JobType?.DisplayName ?? "";
            ws.Cell(row, 2).Value = staff?.EmploymentType?.DisplayName ?? "";
            ws.Cell(row, 3).Value = staff?.DisplayName ?? "";
            ws.Cell(row, 4).Value = staff?.Qualification?.DisplayName ?? "";
            ws.Cell(row, 5).Value = eval.WorkDayCount;
            ws.Cell(row, 6).Value = eval.DayOffCount;
            ws.Cell(row, 7).Value = eval.NightDutyCount;
            ws.Cell(row, 8).Value = eval.EarlyCount;
            ws.Cell(row, 9).Value = eval.LateCount;
            ws.Cell(row, 10).Value = eval.DayCount;
            ws.Cell(row, 11).Value = eval.Floor2Count;
            ws.Cell(row, 12).Value = eval.Floor3Count;
            ws.Cell(row, 13).Value = eval.WorkloadScore;
            ws.Cell(row, 14).Value = eval.SoftAchievedCount;
            ws.Cell(row, 15).Value = eval.SoftViolationCount;

            if (row % 2 == 0)
            {
                ws.Range(row, 1, row, headers.Length).Style.Fill.BackgroundColor = XLColor.FromHtml("#F2F4F8");
            }
            row++;
        }

        ws.Columns().AdjustToContents();
    }

    private void PopulateConstraintSheet(
        IXLWorksheet ws,
        ScheduleCase scheduleCase,
        OptimizationInput input)
    {
        ws.Cell("A1").Value = "適用された制約条件一覧";
        ws.Cell("A1").Style.Font.Bold = true;
        ws.Cell("A1").Style.Font.FontSize = 14;

        // 施設共通条件
        ws.Cell("A3").Value = "【施設基本条件】";
        ws.Cell("A3").Style.Font.Bold = true;
        ws.Cell("A4").Value = $"月間最低休日数: {input.Settings.MinDaysOff}日";
        ws.Cell("A5").Value = $"最大連続勤務日数: {input.Settings.MaxConsecutiveWorkDays}日";
        ws.Cell("A6").Value = "夜勤シーケンス: 夜勤入り(2－/3－) -> 夜勤明け(－) -> 休み(ヤ/有) 【必須】";
        ws.Cell("A7").Value = "日曜日休日: 月に最低1回の日曜日は休み 【必須】";

        // 個人希望一覧
        ws.Cell("A9").Value = "【個人勤務希望・条件一覧】";
        ws.Cell("A9").Style.Font.Bold = true;

        string[] headers = { "氏名", "対象日", "条件種類", "制約レベル", "優先度", "状況", "備考" };
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(10, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#595959");
            cell.Style.Font.FontColor = XLColor.White;
        }

        var staffMap = input.AllStaff.ToDictionary(s => s.Id);
        int row = 11;

        foreach (var c in input.PersonalConstraints.OrderBy(c => c.StaffId).ThenBy(c => c.TargetDate))
        {
            var staff = staffMap.GetValueOrDefault(c.StaffId);
            ws.Cell(row, 1).Value = staff?.DisplayName ?? "";
            ws.Cell(row, 2).Value = c.TargetDate.HasValue ? $"{c.TargetDate.Value:yyyy/MM/dd}" : "月全体";
            ws.Cell(row, 3).Value = c.Type.ToString();
            ws.Cell(row, 4).Value = c.Level == ConstraintLevel.Hard ? "Hard(必須)" : "Soft(要望)";
            ws.Cell(row, 5).Value = c.Priority;
            ws.Cell(row, 6).Value = "達成"; // デフォルト
            ws.Cell(row, 7).Value = c.Note ?? "";
            row++;
        }

        ws.Columns().AdjustToContents();
    }

    private void PopulateDailySummarySheet(
        IXLWorksheet ws,
        ScheduleCase scheduleCase,
        OptimizationInput input)
    {
        ws.Cell("A1").Value = "日別勤務形態別配置人数集計";
        ws.Cell("A1").Style.Font.Bold = true;
        ws.Cell("A1").Style.Font.FontSize = 14;

        int daysInMonth = input.Schedule.DaysInMonth;
        int year = input.Schedule.Year;
        int month = input.Schedule.Month;

        var shiftTypeMap = input.ShiftTypes.ToDictionary(t => t.Id);
        var assignmentsByDate = scheduleCase.Assignments
            .GroupBy(a => a.Date)
            .ToDictionary(g => g.Key, g => g.ToList());

        var requiredShifts = input.ShiftTypes
            .Where(s => s.Code is "EARLY_2F" or "LATE_2F" or "NIGHT_2F" or "EARLY_3F" or "LATE_3F" or "NIGHT_3F" or "DAY")
            .OrderBy(s => s.DisplayOrder)
            .ToList();

        // ヘッダー
        ws.Cell(3, 1).Value = "勤務種類";
        ws.Cell(3, 2).Value = "必要";
        for (int d = 1; d <= daysInMonth; d++)
        {
            ws.Cell(3, 2 + d).Value = $"{d}日";
        }

        int row = 4;
        foreach (var st in requiredShifts)
        {
            ws.Cell(row, 1).Value = $"{st.DisplaySymbol} ({st.DisplayName})";
            var req = input.Requirements.FirstOrDefault(r => r.ShiftTypeId == st.Id);
            int reqCount = req?.RequiredCount ?? 1;
            ws.Cell(row, 2).Value = reqCount;

            for (int d = 1; d <= daysInMonth; d++)
            {
                var date = new DateOnly(year, month, d);
                var dayAssignments = assignmentsByDate.GetValueOrDefault(date, new List<ShiftAssignment>());
                int count = dayAssignments.Count(a => a.ShiftTypeId == st.Id);

                var cell = ws.Cell(row, 2 + d);
                cell.Value = count;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                if (count < reqCount)
                {
                    cell.Style.Fill.BackgroundColor = XLColor.LightPink;
                    cell.Style.Font.FontColor = XLColor.Red;
                }
            }
            row++;
        }

        ws.Columns().AdjustToContents();
    }
}
