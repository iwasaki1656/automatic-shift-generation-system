using Microsoft.EntityFrameworkCore;
using ShiftManagement.Core.Interfaces;
using ShiftManagement.Core.Models;

namespace ShiftManagement.Data.Repositories;

public class ScheduleRepository : IScheduleRepository
{
    private readonly ShiftDbContext _context;

    public ScheduleRepository(ShiftDbContext context)
    {
        _context = context;
    }

    public async Task<MonthlySchedule?> GetByYearMonthAsync(int year, int month)
    {
        return await _context.MonthlySchedules
            .Include(s => s.Cases)
                .ThenInclude(c => c.Assignments)
                    .ThenInclude(a => a.ShiftType)
            .Include(s => s.Cases)
                .ThenInclude(c => c.StaffEvaluations)
            .Include(s => s.Cases)
                .ThenInclude(c => c.ValidationResult)
            .FirstOrDefaultAsync(s => s.Year == year && s.Month == month);
    }

    public async Task<MonthlySchedule> CreateOrGetAsync(int year, int month)
    {
        var existing = await GetByYearMonthAsync(year, month);
        if (existing != null)
        {
            return existing;
        }

        var schedule = new MonthlySchedule
        {
            Year = year,
            Month = month,
            Status = ScheduleStatus.Draft,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.MonthlySchedules.Add(schedule);
        await _context.SaveChangesAsync();
        return schedule;
    }

    public async Task SaveCaseAsync(ScheduleCase scheduleCase)
    {
        var existingCase = await _context.ScheduleCases
            .Include(c => c.Assignments)
            .Include(c => c.StaffEvaluations)
            .Include(c => c.ValidationResult)
            .FirstOrDefaultAsync(c => c.Id == scheduleCase.Id);

        if (existingCase != null)
        {
            _context.ShiftAssignments.RemoveRange(existingCase.Assignments);
            _context.StaffEvaluations.RemoveRange(existingCase.StaffEvaluations);
            if (existingCase.ValidationResult != null)
            {
                _context.ValidationResults.Remove(existingCase.ValidationResult);
            }

            existingCase.CaseName = scheduleCase.CaseName;
            existingCase.TotalScore = scheduleCase.TotalScore;
            existingCase.HardViolationCount = scheduleCase.HardViolationCount;
            existingCase.SoftViolationCount = scheduleCase.SoftViolationCount;
            existingCase.PersonalWishAchievementRate = scheduleCase.PersonalWishAchievementRate;
            existingCase.NightDutyVariance = scheduleCase.NightDutyVariance;
            existingCase.WorkloadVariance = scheduleCase.WorkloadVariance;
            existingCase.IsSelected = scheduleCase.IsSelected;
            existingCase.SolverTimeSeconds = scheduleCase.SolverTimeSeconds;
            existingCase.Assignments = scheduleCase.Assignments;
            existingCase.StaffEvaluations = scheduleCase.StaffEvaluations;
            existingCase.ValidationResult = scheduleCase.ValidationResult;
        }
        else
        {
            _context.ScheduleCases.Add(scheduleCase);
        }

        await _context.SaveChangesAsync();
    }

    public async Task<List<MonthlySchedule>> GetAllAsync()
    {
        return await _context.MonthlySchedules
            .OrderByDescending(s => s.Year)
            .ThenByDescending(s => s.Month)
            .ToListAsync();
    }

    public async Task UpdateStatusAsync(int scheduleId, ScheduleStatus status)
    {
        var schedule = await _context.MonthlySchedules.FindAsync(scheduleId);
        if (schedule != null)
        {
            schedule.Status = status;
            schedule.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}
