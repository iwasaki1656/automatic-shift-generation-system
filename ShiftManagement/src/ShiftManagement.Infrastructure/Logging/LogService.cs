using Serilog;

namespace ShiftManagement.Infrastructure.Logging;

public static class LogService
{
    public static void Initialize(string? logDirectory = null)
    {
        var logDir = logDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ShiftManagement",
            "Logs");

        Directory.CreateDirectory(logDir);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Debug()
            .WriteTo.File(
                Path.Combine(logDir, "shift_management_.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        Log.Information("=== アプリケーション起動: ログ記録開始 ===");
    }

    public static void CloseAndFlush()
    {
        Log.Information("=== アプリケーション終了 ===");
        Log.CloseAndFlush();
    }
}
