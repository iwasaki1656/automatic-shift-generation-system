using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShiftManagement.Core.Interfaces;
using ShiftManagement.Core.Models;

namespace ShiftManagement.App.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly IScheduleRepository _scheduleRepo;
    private readonly IStaffRepository _staffRepo;
    private readonly IPersonalConstraintRepository _constraintRepo;
    private readonly IFacilitySettingsRepository _settingsRepo;

    [ObservableProperty]
    private int _year = DateTime.Today.Year;

    [ObservableProperty]
    private int _month = DateTime.Today.Month;

    [ObservableProperty]
    private string _facilityName = "";

    [ObservableProperty]
    private string _scheduleStatus = "未生成（下書き）";

    [ObservableProperty]
    private int _activeStaffCount;

    [ObservableProperty]
    private int _hardConstraintCount;

    [ObservableProperty]
    private int _softConstraintCount;

    [ObservableProperty]
    private double _latestScore;

    [ObservableProperty]
    private string _latestCaseName = "未生成";

    [ObservableProperty]
    private bool _hasGeneratedCase;

    public DashboardViewModel(
        IScheduleRepository scheduleRepo,
        IStaffRepository staffRepo,
        IPersonalConstraintRepository constraintRepo,
        IFacilitySettingsRepository settingsRepo)
    {
        _scheduleRepo = scheduleRepo;
        _staffRepo = staffRepo;
        _constraintRepo = constraintRepo;
        _settingsRepo = settingsRepo;
    }

    [RelayCommand]
    public async Task LoadDataAsync()
    {
        var settings = await _settingsRepo.GetAsync();
        FacilityName = settings.FacilityName;

        var staffList = await _staffRepo.GetAllActiveAsync();
        ActiveStaffCount = staffList.Count;

        var schedule = await _scheduleRepo.GetByYearMonthAsync(Year, Month);
        if (schedule != null)
        {
            ScheduleStatus = schedule.Status switch
            {
                Core.Models.ScheduleStatus.Draft => "下書き",
                Core.Models.ScheduleStatus.Generated => "生成済み（検討中）",
                Core.Models.ScheduleStatus.Finalized => "確定済み",
                _ => "不明"
            };

            var constraints = await _constraintRepo.GetByScheduleAsync(schedule.Id);
            HardConstraintCount = constraints.Count(c => c.Level == ConstraintLevel.Hard);
            SoftConstraintCount = constraints.Count(c => c.Level == ConstraintLevel.Soft);

            var selectedCase = schedule.Cases.FirstOrDefault(c => c.IsSelected) ?? schedule.Cases.FirstOrDefault();
            if (selectedCase != null)
            {
                HasGeneratedCase = true;
                LatestScore = selectedCase.TotalScore ?? 0.0;
                LatestCaseName = selectedCase.CaseName;
            }
            else
            {
                HasGeneratedCase = false;
                LatestScore = 0;
                LatestCaseName = "未生成";
            }
        }
        else
        {
            ScheduleStatus = "未作成";
            HardConstraintCount = 0;
            SoftConstraintCount = 0;
            HasGeneratedCase = false;
            LatestScore = 0;
            LatestCaseName = "未生成";
        }
    }

    [RelayCommand]
    public async Task ChangeMonthAsync(int delta)
    {
        var date = new DateTime(Year, Month, 1).AddMonths(delta);
        Year = date.Year;
        Month = date.Month;
        await LoadDataAsync();
    }
}
