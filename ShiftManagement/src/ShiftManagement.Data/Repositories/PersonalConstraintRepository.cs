using Microsoft.EntityFrameworkCore;
using ShiftManagement.Core.Interfaces;
using ShiftManagement.Core.Models;

namespace ShiftManagement.Data.Repositories;

public class PersonalConstraintRepository : IPersonalConstraintRepository
{
    private readonly ShiftDbContext _context;

    public PersonalConstraintRepository(ShiftDbContext context)
    {
        _context = context;
    }

    public async Task<List<PersonalConstraint>> GetByScheduleAsync(int scheduleId)
    {
        return await _context.PersonalConstraints
            .Include(c => c.Staff)
            .Include(c => c.ShiftType)
            .Where(c => c.MonthlyScheduleId == scheduleId)
            .OrderBy(c => c.TargetDate)
            .ThenBy(c => c.StaffId)
            .ToListAsync();
    }

    public async Task<List<PersonalConstraint>> GetByStaffAsync(int scheduleId, int staffId)
    {
        return await _context.PersonalConstraints
            .Include(c => c.ShiftType)
            .Where(c => c.MonthlyScheduleId == scheduleId && c.StaffId == staffId)
            .OrderBy(c => c.TargetDate)
            .ToListAsync();
    }

    public async Task SaveAsync(PersonalConstraint constraint)
    {
        if (constraint.Id == 0)
        {
            constraint.CreatedAt = DateTime.UtcNow;
            constraint.UpdatedAt = DateTime.UtcNow;
            _context.PersonalConstraints.Add(constraint);
        }
        else
        {
            constraint.UpdatedAt = DateTime.UtcNow;
            _context.PersonalConstraints.Update(constraint);
        }

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var item = await _context.PersonalConstraints.FindAsync(id);
        if (item != null)
        {
            _context.PersonalConstraints.Remove(item);
            await _context.SaveChangesAsync();
        }
    }

    public async Task BulkSaveAsync(IEnumerable<PersonalConstraint> constraints)
    {
        var now = DateTime.UtcNow;
        foreach (var c in constraints)
        {
            if (c.Id == 0)
            {
                c.CreatedAt = now;
                c.UpdatedAt = now;
                _context.PersonalConstraints.Add(c);
            }
            else
            {
                c.UpdatedAt = now;
                _context.PersonalConstraints.Update(c);
            }
        }
        await _context.SaveChangesAsync();
    }
}
