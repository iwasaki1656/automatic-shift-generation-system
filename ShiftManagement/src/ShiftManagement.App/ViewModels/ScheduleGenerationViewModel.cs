using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShiftManagement.Core.Interfaces;
using ShiftManagement.Core.Models;

namespace ShiftManagement.App.ViewModels;

public partial class ScheduleGenerationViewModel : ObservableObject
{
    private readonly IShiftOptimizer _optimizer;
    private readonly IScheduleRepository _scheduleRepo;
    private readonly IStaffRepository _staffRepo;
    private readonly IShiftTypeRepository _shiftTypeRepo;
    private readonly IPersonalConstraintRepository _constraintRepo;
    private readonly IFacilitySettingsRepository _settingsRepo;

    [ObservableProperty]
    private int _year = DateTime.Today.Year;

    [ObservableProperty]
    private int _month = DateTime.Today.Month;

    [ObservableProperty]
    private bool _isGenerating;

    [ObservableProperty]
    private double _progressValue;

    [ObservableProperty]
    private string _progressMessage = "待機中";

    [ObservableProperty]
    private string _resultStatusMessage = "";

    [ObservableProperty]
    private ObservableCollection<ScheduleCase> _generatedCases = new();

    [ObservableProperty]
    private ScheduleCase? _selectedCase;

    [ObservableProperty]
    private bool _hasConflictAnalysis;

    [ObservableProperty]
    private ConflictAnalysis? _conflictAnalysis;

    private CancellationTokenSource? _cts;

    public ScheduleGenerationViewModel(
        IShiftOptimizer optimizer,
        IScheduleRepository scheduleRepo,
        IStaffRepository staffRepo,
        IShiftTypeRepository shiftTypeRepo,
        IPersonalConstraintRepository constraintRepo,
        IFacilitySettingsRepository settingsRepo)
    {
        _optimizer = optimizer;
        _scheduleRepo = scheduleRepo;
        _staffRepo = staffRepo;
        _shiftTypeRepo = shiftTypeRepo;
        _constraintRepo = constraintRepo;
        _settingsRepo = settingsRepo;
    }

    [RelayCommand]
    public async Task StartGenerationAsync()
    {
        IsGenerating = true;
        ProgressValue = 0;
        ProgressMessage = "最適化準備中...";
        ResultStatusMessage = "";
        HasConflictAnalysis = false;
        ConflictAnalysis = null;
        GeneratedCases.Clear();

        _cts = new CancellationTokenSource();

        try
        {
            var schedule = await _scheduleRepo.CreateOrGetAsync(Year, Month);
            var staffList = await _staffRepo.GetAllActiveAsync();
            var shiftTypes = await _shiftTypeRepo.GetAllAsync();
            var settings = await _settingsRepo.GetAsync();
            var constraints = await _constraintRepo.GetByScheduleAsync(schedule.Id);

            // デフォルト必要人数設定
            var reqs = new List<ShiftRequirement>();
            var requiredCodes = new[] { "EARLY_2F", "LATE_2F", "NIGHT_2F", "EARLY_3F", "LATE_3F", "NIGHT_3F", "DAY" };
            foreach (var code in requiredCodes)
            {
                var st = shiftTypes.FirstOrDefault(s => s.Code == code);
                if (st != null)
                {
                    reqs.Add(new ShiftRequirement { ShiftTypeId = st.Id, RequiredCount = 1, IsActive = true });
                }
            }

            var input = new OptimizationInput
            {
                Schedule = schedule,
                AllStaff = staffList,
                ShiftTypes = shiftTypes,
                Requirements = reqs,
                PersonalConstraints = constraints,
                Settings = settings
            };

            var progress = new Progress<OptimizationProgress>(p =>
            {
                ProgressValue = p.ProgressPercentage;
                ProgressMessage = p.Message;
            });

            var result = await _optimizer.OptimizeAsync(input, progress, _cts.Token);

            if (result.IsSuccess)
            {
                ResultStatusMessage = $"勤務表の自動生成に成功しました！（{result.Cases.Count}案作成、所要時間: {result.TotalTimeSeconds:F1}秒）";
                GeneratedCases = new ObservableCollection<ScheduleCase>(result.Cases);

                // DBに保存
                foreach (var c in result.Cases)
                {
                    c.MonthlyScheduleId = schedule.Id;
                    await _scheduleRepo.SaveCaseAsync(c);
                }
                await _scheduleRepo.UpdateStatusAsync(schedule.Id, ScheduleStatus.Generated);
            }
            else
            {
                ResultStatusMessage = result.ErrorMessage ?? "生成に失敗しました。";
                if (result.ConflictAnalysis != null)
                {
                    HasConflictAnalysis = true;
                    ConflictAnalysis = result.ConflictAnalysis;
                }
            }
        }
        catch (OperationCanceledException)
        {
            ResultStatusMessage = "生成処理がキャンセルされました。";
        }
        catch (Exception ex)
        {
            ResultStatusMessage = $"エラーが発生しました: {ex.Message}";
        }
        finally
        {
            IsGenerating = false;
        }
    }

    [RelayCommand]
    public void CancelGeneration()
    {
        _cts?.Cancel();
    }
}
