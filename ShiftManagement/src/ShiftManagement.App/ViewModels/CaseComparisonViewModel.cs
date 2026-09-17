using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShiftManagement.Core.Interfaces;
using ShiftManagement.Core.Models;

namespace ShiftManagement.App.ViewModels;

public partial class CaseComparisonViewModel : ObservableObject
{
    private readonly IScheduleRepository _scheduleRepo;

    [ObservableProperty]
    private int _year = DateTime.Today.Year;

    [ObservableProperty]
    private int _month = DateTime.Today.Month;

    [ObservableProperty]
    private ObservableCollection<ScheduleCase> _cases = new();

    [ObservableProperty]
    private ScheduleCase? _selectedCase;

    [ObservableProperty]
    private string _statusMessage = "";

    public CaseComparisonViewModel(IScheduleRepository scheduleRepo)
    {
        _scheduleRepo = scheduleRepo;
    }

    [RelayCommand]
    public async Task LoadCasesAsync()
    {
        var schedule = await _scheduleRepo.GetByYearMonthAsync(Year, Month);
        if (schedule != null && schedule.Cases.Count > 0)
        {
            Cases = new ObservableCollection<ScheduleCase>(schedule.Cases.OrderBy(c => c.CaseNumber));
            SelectedCase = Cases.FirstOrDefault(c => c.IsSelected) ?? Cases.FirstOrDefault();
            StatusMessage = $"{Cases.Count}つの案が読み込まれました。";
        }
        else
        {
            Cases.Clear();
            SelectedCase = null;
            StatusMessage = "保存された案がありません。自動生成画面で生成を行ってください。";
        }
    }

    [RelayCommand]
    public async Task AdoptCaseAsync(ScheduleCase targetCase)
    {
        foreach (var c in Cases)
        {
            c.IsSelected = (c.Id == targetCase.Id);
            await _scheduleRepo.SaveCaseAsync(c);
        }
        SelectedCase = targetCase;
        StatusMessage = $"{targetCase.CaseName} を正式採用案として選択しました。";
    }
}
