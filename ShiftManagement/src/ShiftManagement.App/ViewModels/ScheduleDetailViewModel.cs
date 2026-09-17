using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShiftManagement.Core.Interfaces;
using ShiftManagement.Core.Models;
using ShiftManagement.Excel;
using ShiftManagement.Infrastructure.UndoRedo;

namespace ShiftManagement.App.ViewModels;

public class ScheduleGridCell : ObservableObject
{
    public int StaffId { get; set; }
    public DateOnly Date { get; set; }

    private ShiftType? _shiftType;
    public ShiftType? ShiftType
    {
        get => _shiftType;
        set
        {
            if (SetProperty(ref _shiftType, value))
            {
                OnPropertyChanged(nameof(DisplaySymbol));
                OnPropertyChanged(nameof(BackgroundColor));
                OnPropertyChanged(nameof(FontColor));
            }
        }
    }

    public string DisplaySymbol => ShiftType?.DisplaySymbol ?? "";
    public string BackgroundColor => ShiftType?.ExcelBackgroundColor ?? "#FFFFFF";
    public string FontColor => ShiftType?.ExcelFontColor ?? "#000000";

    private bool _hasHardViolation;
    public bool HasHardViolation
    {
        get => _hasHardViolation;
        set => SetProperty(ref _hasHardViolation, value);
    }

    private bool _hasSoftViolation;
    public bool HasSoftViolation
    {
        get => _hasSoftViolation;
        set => SetProperty(ref _hasSoftViolation, value);
    }

    private string _tooltipText = "";
    public string TooltipText
    {
        get => _tooltipText;
        set => SetProperty(ref _tooltipText, value);
    }

    public bool IsSunday => Date.DayOfWeek == DayOfWeek.Sunday;
    public bool IsSaturday => Date.DayOfWeek == DayOfWeek.Saturday;
}

public class ScheduleGridRow : ObservableObject
{
    public Staff Staff { get; set; } = null!;
    public ObservableCollection<ScheduleGridCell> Cells { get; set; } = new();

    // 集計
    public int OffCount { get; set; }
    public int PaidOffCount { get; set; }
    public int NightCount { get; set; }
    public int DayCount { get; set; }
    public int EarlyCount { get; set; }
    public int LateCount { get; set; }
    public double WorkloadScore { get; set; }
}

public partial class ScheduleDetailViewModel : ObservableObject
{
    private readonly IScheduleRepository _scheduleRepo;
    private readonly IStaffRepository _staffRepo;
    private readonly IShiftTypeRepository _shiftTypeRepo;
    private readonly IShiftValidator _validator;
    private readonly IFacilitySettingsRepository _settingsRepo;
    private readonly IPersonalConstraintRepository _constraintRepo;
    private readonly UndoRedoManager _undoRedo = new();

    [ObservableProperty]
    private int _year = DateTime.Today.Year;

    [ObservableProperty]
    private int _month = DateTime.Today.Month;

    [ObservableProperty]
    private ScheduleCase? _currentCase;

    [ObservableProperty]
    private ObservableCollection<ScheduleGridRow> _gridRows = new();

    [ObservableProperty]
    private ObservableCollection<int> _days = new();

    [ObservableProperty]
    private ObservableCollection<ShiftType> _availableShiftTypes = new();

    [ObservableProperty]
    private ScheduleGridCell? _selectedCell;

    [ObservableProperty]
    private string _validationSummary = "";

    [ObservableProperty]
    private string _statusMessage = "";

    [ObservableProperty]
    private bool _canUndo;

    [ObservableProperty]
    private bool _canRedo;

    public ScheduleDetailViewModel(
        IScheduleRepository scheduleRepo,
        IStaffRepository staffRepo,
        IShiftTypeRepository shiftTypeRepo,
        IShiftValidator validator,
        IFacilitySettingsRepository settingsRepo,
        IPersonalConstraintRepository constraintRepo)
    {
        _scheduleRepo = scheduleRepo;
        _staffRepo = staffRepo;
        _shiftTypeRepo = shiftTypeRepo;
        _validator = validator;
        _settingsRepo = settingsRepo;
        _constraintRepo = constraintRepo;

        _undoRedo.StateChanged += (_, _) =>
        {
            CanUndo = _undoRedo.CanUndo;
            CanRedo = _undoRedo.CanRedo;
        };
    }

    [RelayCommand]
    public async Task LoadScheduleAsync()
    {
        var schedule = await _scheduleRepo.GetByYearMonthAsync(Year, Month);
        if (schedule == null || schedule.Cases.Count == 0)
        {
            StatusMessage = $"{Year}年{Month}月の勤務表データがありません。先に自動生成を行ってください。";
            GridRows.Clear();
            return;
        }

        CurrentCase = schedule.Cases.FirstOrDefault(c => c.IsSelected) ?? schedule.Cases.First();
        var shifts = await _shiftTypeRepo.GetAllAsync();
        AvailableShiftTypes = new ObservableCollection<ShiftType>(shifts);

        int daysInMonth = DateTime.DaysInMonth(Year, Month);
        Days = new ObservableCollection<int>(Enumerable.Range(1, daysInMonth));

        var assignmentMap = CurrentCase.Assignments.ToDictionary(a => (a.StaffId, a.Date));
        var rows = new List<ScheduleGridRow>();

        var staffList = await _staffRepo.GetAllActiveAsync();
        foreach (var staff in staffList)
        {
            var row = new ScheduleGridRow { Staff = staff };
            for (int d = 1; d <= daysInMonth; d++)
            {
                var date = new DateOnly(Year, Month, d);
                assignmentMap.TryGetValue((staff.Id, date), out var a);

                var cell = new ScheduleGridCell
                {
                    StaffId = staff.Id,
                    Date = date,
                    ShiftType = a?.ShiftType ?? shifts.FirstOrDefault(s => s.Id == a?.ShiftTypeId)
                };

                row.Cells.Add(cell);
            }
            CalculateRowStats(row);
            rows.Add(row);
        }

        GridRows = new ObservableCollection<ScheduleGridRow>(rows);
        RunValidationCheck();
        StatusMessage = $"{CurrentCase.CaseName} を表示中（スコア: {CurrentCase.TotalScore:F1}点）";
    }

    private void CalculateRowStats(ScheduleGridRow row)
    {
        row.OffCount = row.Cells.Count(c => c.ShiftType?.Code == "OFF");
        row.PaidOffCount = row.Cells.Count(c => c.ShiftType?.Code == "PAID_OFF");
        row.NightCount = row.Cells.Count(c => c.ShiftType?.ShiftCategory == ShiftCategory.NightIn);
        row.DayCount = row.Cells.Count(c => c.ShiftType?.Code == "DAY");
        row.EarlyCount = row.Cells.Count(c => c.ShiftType?.ShiftCategory == ShiftCategory.Early);
        row.LateCount = row.Cells.Count(c => c.ShiftType?.ShiftCategory == ShiftCategory.Late);
        row.WorkloadScore = row.Cells.Sum(c => c.ShiftType?.LoadFactor ?? 0);
    }

    [RelayCommand]
    public void ChangeCellShift(ShiftType newShift)
    {
        if (SelectedCell == null || CurrentCase == null) return;

        var cell = SelectedCell;
        var oldShift = cell.ShiftType;

        var action = new ShiftChangeAction(
            cell,
            oldShift,
            newShift,
            () =>
            {
                var row = GridRows.FirstOrDefault(r => r.Staff.Id == cell.StaffId);
                if (row != null) CalculateRowStats(row);
                RunValidationCheck();
            });

        _undoRedo.ExecuteAction(action);
    }

    [RelayCommand]
    public void Undo() => _undoRedo.Undo();

    [RelayCommand]
    public void Redo() => _undoRedo.Redo();

    private void RunValidationCheck()
    {
        // 全セルから現在の割り当てを再構成して検証
        var assignments = new List<ShiftAssignment>();
        foreach (var row in GridRows)
        {
            foreach (var cell in row.Cells)
            {
                if (cell.ShiftType != null)
                {
                    assignments.Add(new ShiftAssignment
                    {
                        StaffId = cell.StaffId,
                        Staff = row.Staff,
                        Date = cell.Date,
                        ShiftTypeId = cell.ShiftType.Id,
                        ShiftType = cell.ShiftType
                    });
                }
            }
        }

        var input = new OptimizationInput
        {
            Schedule = new MonthlySchedule { Year = Year, Month = Month },
            AllStaff = GridRows.Select(r => r.Staff).ToList(),
            ShiftTypes = AvailableShiftTypes.ToList(),
            Settings = new FacilitySettings { MinDaysOff = 9, MaxConsecutiveWorkDays = 5 }
        };

        var result = _validator.Validate(assignments, input);
        ValidationSummary = result.Summary;

        // セル違反フラグのリセットと反映
        var hardViolationKeys = result.HardViolations
            .Where(v => v.StaffId.HasValue && v.Date.HasValue)
            .ToDictionary(v => (v.StaffId!.Value, v.Date!.Value), v => v.Message);

        var softViolationKeys = result.SoftViolations
            .Where(v => v.StaffId.HasValue && v.Date.HasValue)
            .ToDictionary(v => (v.StaffId!.Value, v.Date!.Value), v => v.Message);

        foreach (var row in GridRows)
        {
            foreach (var cell in row.Cells)
            {
                var key = (cell.StaffId, cell.Date);
                if (hardViolationKeys.TryGetValue(key, out var hMsg))
                {
                    cell.HasHardViolation = true;
                    cell.TooltipText = $"【Hard違反】{hMsg}";
                }
                else if (softViolationKeys.TryGetValue(key, out var sMsg))
                {
                    cell.HasSoftViolation = true;
                    cell.TooltipText = $"【Soft未達成】{sMsg}";
                }
                else
                {
                    cell.HasHardViolation = false;
                    cell.HasSoftViolation = false;
                    cell.TooltipText = $"{cell.Date:M/d} {cell.ShiftType?.DisplayName}";
                }
            }
        }
    }

    [RelayCommand]
    public async Task ExportExcelAsync(string? targetFilePath = null)
    {
        if (CurrentCase == null) return;

        var savePath = targetFilePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            $"{Year}年{Month}月_勤務表_{CurrentCase.CaseName}.xlsx");

        var templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "Template.xlsx");
        var exporter = new ExcelExporter(templatePath);

        var staff = GridRows.Select(r => r.Staff).ToList();
        var input = new OptimizationInput
        {
            Schedule = new MonthlySchedule { Year = Year, Month = Month },
            AllStaff = staff,
            ShiftTypes = AvailableShiftTypes.ToList(),
            Settings = await _settingsRepo.GetAsync(),
            PersonalConstraints = await _constraintRepo.GetByScheduleAsync(CurrentCase.MonthlyScheduleId)
        };

        await exporter.ExportScheduleAsync(CurrentCase, input, savePath);
        StatusMessage = $"Excelファイルを出力しました: {savePath}";
    }

    private class ShiftChangeAction : IUndoableAction
    {
        private readonly ScheduleGridCell _cell;
        private readonly ShiftType? _oldShift;
        private readonly ShiftType? _newShift;
        private readonly Action _callback;

        public string Description => $"{_cell.Date:M/d}の勤務を {_newShift?.DisplayName} に変更";

        public ShiftChangeAction(ScheduleGridCell cell, ShiftType? oldShift, ShiftType? newShift, Action callback)
        {
            _cell = cell;
            _oldShift = oldShift;
            _newShift = newShift;
            _callback = callback;
        }

        public void Redo()
        {
            _cell.ShiftType = _newShift;
            _callback();
        }

        public void Undo()
        {
            _cell.ShiftType = _oldShift;
            _callback();
        }
    }
}
