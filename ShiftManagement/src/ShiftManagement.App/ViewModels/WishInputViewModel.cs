using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShiftManagement.Core.Interfaces;
using ShiftManagement.Core.Models;

namespace ShiftManagement.App.ViewModels;

public class WishCalendarDayItem : ObservableObject
{
    public DateOnly Date { get; set; }
    public int Day => Date.Day;
    public string DayOfWeekString => Date.DayOfWeek switch
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
    public bool IsSunday => Date.DayOfWeek == DayOfWeek.Sunday;
    public bool IsSaturday => Date.DayOfWeek == DayOfWeek.Saturday;

    private string _wishTypeDisplay = "なし";
    public string WishTypeDisplay
    {
        get => _wishTypeDisplay;
        set => SetProperty(ref _wishTypeDisplay, value);
    }

    private ConstraintLevel _level = ConstraintLevel.Soft;
    public ConstraintLevel Level
    {
        get => _level;
        set => SetProperty(ref _level, value);
    }

    private int _priority = 3;
    public int Priority
    {
        get => _priority;
        set => SetProperty(ref _priority, value);
    }

    private string? _note;
    public string? Note
    {
        get => _note;
        set => SetProperty(ref _note, value);
    }

    public PersonalConstraint? UnderlyingConstraint { get; set; }
}

public partial class WishInputViewModel : ObservableObject
{
    private readonly IScheduleRepository _scheduleRepo;
    private readonly IStaffRepository _staffRepo;
    private readonly IPersonalConstraintRepository _constraintRepo;

    [ObservableProperty]
    private int _year = DateTime.Today.Year;

    [ObservableProperty]
    private int _month = DateTime.Today.Month;

    [ObservableProperty]
    private ObservableCollection<Staff> _staffList = new();

    [ObservableProperty]
    private Staff? _selectedStaff;

    [ObservableProperty]
    private ObservableCollection<WishCalendarDayItem> _calendarDays = new();

    [ObservableProperty]
    private WishCalendarDayItem? _selectedDay;

    [ObservableProperty]
    private string _statusMessage = "";

    public WishInputViewModel(
        IScheduleRepository scheduleRepo,
        IStaffRepository staffRepo,
        IPersonalConstraintRepository constraintRepo)
    {
        _scheduleRepo = scheduleRepo;
        _staffRepo = staffRepo;
        _constraintRepo = constraintRepo;
    }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        var staff = await _staffRepo.GetAllActiveAsync();
        StaffList = new ObservableCollection<Staff>(staff);
        if (StaffList.Count > 0)
        {
            SelectedStaff = StaffList[0];
        }
        await LoadCalendarAsync();
    }

    partial void OnSelectedStaffChanged(Staff? value)
    {
        if (value != null)
        {
            _ = LoadCalendarAsync();
        }
    }

    [RelayCommand]
    public async Task LoadCalendarAsync()
    {
        if (SelectedStaff == null) return;

        var schedule = await _scheduleRepo.CreateOrGetAsync(Year, Month);
        var existingConstraints = await _constraintRepo.GetByStaffAsync(schedule.Id, SelectedStaff.Id);

        int daysInMonth = DateTime.DaysInMonth(Year, Month);
        var items = new List<WishCalendarDayItem>();

        for (int d = 1; d <= daysInMonth; d++)
        {
            var date = new DateOnly(Year, Month, d);
            var constraint = existingConstraints.FirstOrDefault(c => c.TargetDate == date);

            var item = new WishCalendarDayItem
            {
                Date = date,
                UnderlyingConstraint = constraint
            };

            if (constraint != null)
            {
                item.WishTypeDisplay = constraint.Type switch
                {
                    ConstraintType.DayOffRequest => "希望休",
                    ConstraintType.WorkUnavailable => "勤務不可",
                    ConstraintType.NightUnavailable => "夜勤不可",
                    ConstraintType.DayPreference => "日勤希望",
                    ConstraintType.NightPreference => "夜勤希望",
                    _ => constraint.Type.ToString()
                };
                item.Level = constraint.Level;
                item.Priority = constraint.Priority;
                item.Note = constraint.Note;
            }

            items.Add(item);
        }

        CalendarDays = new ObservableCollection<WishCalendarDayItem>(items);
        StatusMessage = $"{SelectedStaff.DisplayName} の希望を読み込みました。";
    }

    [RelayCommand]
    public void SetDayOff(WishCalendarDayItem day)
    {
        day.WishTypeDisplay = "希望休";
        day.Level = ConstraintLevel.Soft;
        day.Priority = 4;
    }

    [RelayCommand]
    public void SetWorkUnavailable(WishCalendarDayItem day)
    {
        day.WishTypeDisplay = "勤務不可";
        day.Level = ConstraintLevel.Hard;
        day.Priority = 5;
    }

    [RelayCommand]
    public void SetNightUnavailable(WishCalendarDayItem day)
    {
        day.WishTypeDisplay = "夜勤不可";
        day.Level = ConstraintLevel.Hard;
        day.Priority = 5;
    }

    [RelayCommand]
    public void SetDayPreference(WishCalendarDayItem day)
    {
        day.WishTypeDisplay = "日勤希望";
        day.Level = ConstraintLevel.Soft;
        day.Priority = 3;
    }

    [RelayCommand]
    public void ClearWish(WishCalendarDayItem day)
    {
        day.WishTypeDisplay = "なし";
        day.Level = ConstraintLevel.Soft;
        day.Priority = 3;
        day.Note = null;
    }

    [RelayCommand]
    public async Task SaveWishesAsync()
    {
        if (SelectedStaff == null) return;

        var schedule = await _scheduleRepo.CreateOrGetAsync(Year, Month);
        var existingConstraints = await _constraintRepo.GetByStaffAsync(schedule.Id, SelectedStaff.Id);

        // 既存を一旦削除して再登録
        foreach (var ec in existingConstraints)
        {
            await _constraintRepo.DeleteAsync(ec.Id);
        }

        var newConstraints = new List<PersonalConstraint>();
        foreach (var day in CalendarDays)
        {
            if (day.WishTypeDisplay == "なし") continue;

            var cType = day.WishTypeDisplay switch
            {
                "希望休" => ConstraintType.DayOffRequest,
                "勤務不可" => ConstraintType.WorkUnavailable,
                "夜勤不可" => ConstraintType.NightUnavailable,
                "日勤希望" => ConstraintType.DayPreference,
                "夜勤希望" => ConstraintType.NightPreference,
                _ => ConstraintType.DayOffRequest
            };

            newConstraints.Add(new PersonalConstraint
            {
                MonthlyScheduleId = schedule.Id,
                StaffId = SelectedStaff.Id,
                Type = cType,
                TargetDate = day.Date,
                Level = day.Level,
                Priority = day.Priority,
                Note = day.Note
            });
        }

        await _constraintRepo.BulkSaveAsync(newConstraints);
        StatusMessage = $"{SelectedStaff.DisplayName} の勤務希望を保存しました（{newConstraints.Count}件）。";
    }
}
