using Microsoft.EntityFrameworkCore;
using ShiftManagement.Core.Interfaces;
using ShiftManagement.Core.Models;

namespace ShiftManagement.Data.Repositories;

public class FacilitySettingsRepository : IFacilitySettingsRepository
{
    private readonly ShiftDbContext _context;

    public FacilitySettingsRepository(ShiftDbContext context)
    {
        _context = context;
    }

    public async Task<FacilitySettings> GetAsync()
    {
        var settings = await _context.FacilitySettings.FirstOrDefaultAsync();
        if (settings == null)
        {
            settings = new FacilitySettings
            {
                FacilityName = "住宅型有料老人ホーム ライフステイむなかた",
                MinDaysOff = 9,
                MaxConsecutiveWorkDays = 5,
                CountNightOffAsRest = false,
                NightDutyLoadFactor = 3.0,
                EarlyDutyLoadFactor = 2.0,
                LateDutyLoadFactor = 2.0,
                DayDutyLoadFactor = 1.0,
                UpdatedAt = DateTime.UtcNow
            };
            _context.FacilitySettings.Add(settings);
            await _context.SaveChangesAsync();
        }
        return settings;
    }

    public async Task SaveAsync(FacilitySettings settings)
    {
        settings.UpdatedAt = DateTime.UtcNow;
        if (settings.Id == 0)
        {
            _context.FacilitySettings.Add(settings);
        }
        else
        {
            _context.FacilitySettings.Update(settings);
        }
        await _context.SaveChangesAsync();
    }
}
