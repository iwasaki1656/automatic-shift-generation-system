using System.IO;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ShiftManagement.App.ViewModels;
using ShiftManagement.Core.Interfaces;
using ShiftManagement.Data;
using ShiftManagement.Data.Repositories;
using ShiftManagement.Infrastructure.Logging;
using ShiftManagement.Optimization;
using ShiftManagement.Validation;

namespace ShiftManagement.App;

public partial class App : Application
{
    public static IServiceProvider ServiceProvider { get; private set; } = null!;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        LogService.Initialize();

        var services = new ServiceCollection();
        ConfigureServices(services);
        ServiceProvider = services.BuildServiceProvider();

        // データベース初期化
        using (var scope = ServiceProvider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ShiftDbContext>();
            await DbInitializer.InitializeAsync(db);
        }

        var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // データベース
        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ShiftManagement",
            "shifts.db");

        var dir = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        services.AddDbContext<ShiftDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}"));

        // リポジトリ
        services.AddScoped<IStaffRepository, StaffRepository>();
        services.AddScoped<IScheduleRepository, ScheduleRepository>();
        services.AddScoped<IPersonalConstraintRepository, PersonalConstraintRepository>();
        services.AddScoped<IFacilitySettingsRepository, FacilitySettingsRepository>();
        services.AddScoped<IShiftTypeRepository, ShiftTypeRepository>();

        // 最適化・バリデーション
        services.AddScoped<IShiftValidator, ShiftValidator>();
        services.AddScoped<IShiftOptimizer, CpSatOptimizer>();

        // ViewModels
        services.AddTransient<MainViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<StaffManagementViewModel>();
        services.AddTransient<WishInputViewModel>();
        services.AddTransient<ScheduleGenerationViewModel>();
        services.AddTransient<ScheduleDetailViewModel>();
        services.AddTransient<CaseComparisonViewModel>();
        services.AddTransient<SettingsViewModel>();

        // Views
        services.AddTransient<MainWindow>();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        LogService.CloseAndFlush();
        base.OnExit(e);
    }
}
