using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShiftManagement.Core.Interfaces;

namespace ShiftManagement.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableObject? _currentViewModel;

    [ObservableProperty]
    private string _currentTitle = "ダッシュボード";

    [ObservableProperty]
    private int _targetYear = DateTime.Today.Year;

    [ObservableProperty]
    private int _targetMonth = DateTime.Today.Month;

    [ObservableProperty]
    private string _statusMessage = "準備完了";

    [ObservableProperty]
    private bool _isBusy;

    public DashboardViewModel DashboardVM { get; }
    public StaffManagementViewModel StaffVM { get; }
    public WishInputViewModel WishVM { get; }
    public ScheduleGenerationViewModel GenerationVM { get; }
    public ScheduleDetailViewModel DetailVM { get; }
    public CaseComparisonViewModel ComparisonVM { get; }
    public SettingsViewModel SettingsVM { get; }

    public MainViewModel(
        DashboardViewModel dashboardVM,
        StaffManagementViewModel staffVM,
        WishInputViewModel wishVM,
        ScheduleGenerationViewModel generationVM,
        ScheduleDetailViewModel detailVM,
        CaseComparisonViewModel comparisonVM,
        SettingsViewModel settingsVM)
    {
        DashboardVM = dashboardVM;
        StaffVM = staffVM;
        WishVM = wishVM;
        GenerationVM = generationVM;
        DetailVM = detailVM;
        ComparisonVM = comparisonVM;
        SettingsVM = settingsVM;

        _currentViewModel = DashboardVM;
    }

    [RelayCommand]
    public void Navigate(string target)
    {
        switch (target)
        {
            case "Dashboard":
                CurrentViewModel = DashboardVM;
                CurrentTitle = "ダッシュボード";
                break;
            case "Staff":
                CurrentViewModel = StaffVM;
                CurrentTitle = "職員管理";
                break;
            case "Wish":
                CurrentViewModel = WishVM;
                CurrentTitle = "勤務希望入力";
                break;
            case "Generation":
                CurrentViewModel = GenerationVM;
                CurrentTitle = "勤務表自動生成";
                break;
            case "Detail":
                CurrentViewModel = DetailVM;
                CurrentTitle = "勤務表詳細・手動修正";
                break;
            case "Comparison":
                CurrentViewModel = ComparisonVM;
                CurrentTitle = "3案比較・選択";
                break;
            case "Settings":
                CurrentViewModel = SettingsVM;
                CurrentTitle = "システム・施設設定";
                break;
        }
    }
}
