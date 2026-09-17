using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShiftManagement.Core.Interfaces;
using ShiftManagement.Core.Models;
using ShiftManagement.Infrastructure.Backup;

namespace ShiftManagement.App.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IFacilitySettingsRepository _settingsRepo;
    private readonly BackupService _backupService = new();

    [ObservableProperty]
    private string _facilityName = "";

    [ObservableProperty]
    private int _minDaysOff = 9;

    [ObservableProperty]
    private int _maxConsecutiveWorkDays = 5;

    [ObservableProperty]
    private bool _countNightOffAsRest;

    [ObservableProperty]
    private double _nightDutyLoad = 3.0;

    [ObservableProperty]
    private double _earlyDutyLoad = 2.0;

    [ObservableProperty]
    private double _lateDutyLoad = 2.0;

    [ObservableProperty]
    private double _dayDutyLoad = 1.0;

    [ObservableProperty]
    private ObservableCollection<BackupFileInfo> _backupList = new();

    [ObservableProperty]
    private string _statusMessage = "";

    public SettingsViewModel(IFacilitySettingsRepository settingsRepo)
    {
        _settingsRepo = settingsRepo;
    }

    [RelayCommand]
    public async Task LoadSettingsAsync()
    {
        var settings = await _settingsRepo.GetAsync();
        FacilityName = settings.FacilityName;
        MinDaysOff = settings.MinDaysOff;
        MaxConsecutiveWorkDays = settings.MaxConsecutiveWorkDays;
        CountNightOffAsRest = settings.CountNightOffAsRest;
        NightDutyLoad = settings.NightDutyLoadFactor;
        EarlyDutyLoad = settings.EarlyDutyLoadFactor;
        LateDutyLoad = settings.LateDutyLoadFactor;
        DayDutyLoad = settings.DayDutyLoadFactor;

        RefreshBackups();
    }

    [RelayCommand]
    public async Task SaveSettingsAsync()
    {
        var settings = await _settingsRepo.GetAsync();
        settings.FacilityName = FacilityName;
        settings.MinDaysOff = MinDaysOff;
        settings.MaxConsecutiveWorkDays = MaxConsecutiveWorkDays;
        settings.CountNightOffAsRest = CountNightOffAsRest;
        settings.NightDutyLoadFactor = NightDutyLoad;
        settings.EarlyDutyLoadFactor = EarlyDutyLoad;
        settings.LateDutyLoadFactor = LateDutyLoad;
        settings.DayDutyLoadFactor = DayDutyLoad;

        await _settingsRepo.SaveAsync(settings);
        StatusMessage = "施設設定を保存しました。";
    }

    [RelayCommand]
    public void CreateBackup()
    {
        try
        {
            var path = _backupService.CreateBackup("手動バックアップ");
            StatusMessage = $"バックアップを作成しました: {Path.GetFileName(path)}";
            RefreshBackups();
        }
        catch (Exception ex)
        {
            StatusMessage = $"バックアップ作成失敗: {ex.Message}";
        }
    }

    [RelayCommand]
    public void RestoreBackup(BackupFileInfo file)
    {
        try
        {
            _backupService.RestoreBackup(file.FilePath);
            StatusMessage = $"{file.FileName} から復元しました。アプリを再起動すると完全に反映されます。";
        }
        catch (Exception ex)
        {
            StatusMessage = $"復元失敗: {ex.Message}";
        }
    }

    private void RefreshBackups()
    {
        var list = _backupService.GetBackupList();
        BackupList = new ObservableCollection<BackupFileInfo>(list);
    }
}
