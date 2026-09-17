using Serilog;

namespace ShiftManagement.Infrastructure.AutoSave;

public class AutoSaveService : IDisposable
{
    private readonly Func<Task> _saveAction;
    private readonly TimeSpan _interval;
    private readonly System.Timers.Timer _timer;
    private bool _isDirty;
    private readonly object _lock = new();

    public AutoSaveService(Func<Task> saveAction, TimeSpan? interval = null)
    {
        _saveAction = saveAction;
        _interval = interval ?? TimeSpan.FromMinutes(2);

        _timer = new System.Timers.Timer(_interval.TotalMilliseconds);
        _timer.Elapsed += async (_, _) => await OnTimerElapsedAsync();
        _timer.AutoReset = true;
    }

    public void Start()
    {
        _timer.Start();
        Log.Information("自動保存サービス開始（間隔: {Interval}）", _interval);
    }

    public void Stop()
    {
        _timer.Stop();
        Log.Information("自動保存サービス停止");
    }

    public void MarkDirty()
    {
        lock (_lock)
        {
            _isDirty = true;
        }
    }

    private async Task OnTimerElapsedAsync()
    {
        bool shouldSave;
        lock (_lock)
        {
            shouldSave = _isDirty;
            _isDirty = false;
        }

        if (shouldSave)
        {
            try
            {
                Log.Debug("自動保存を実行中...");
                await _saveAction();
                Log.Information("自動保存が正常に完了しました。");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "自動保存中にエラーが発生しました。");
                lock (_lock)
                {
                    _isDirty = true; // 次回リトライ
                }
            }
        }
    }

    public void Dispose()
    {
        _timer.Dispose();
    }
}
