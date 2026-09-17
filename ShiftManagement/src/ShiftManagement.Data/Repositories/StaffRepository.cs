using Microsoft.EntityFrameworkCore;
using ShiftManagement.Core.Interfaces;
using ShiftManagement.Core.Models;

namespace ShiftManagement.Data.Repositories;

public class StaffRepository : IStaffRepository
{
    private readonly ShiftDbContext _context;

    public StaffRepository(ShiftDbContext context)
    {
        _context = context;
    }

    public async Task<List<Staff>> GetAllActiveAsync()
    {
        return await _context.Staff
            .Include(s => s.JobType)
            .Include(s => s.EmploymentType)
            .Include(s => s.Qualification)
            .Include(s => s.PermanentConstraints)
            .Where(s => s.IsActive)
            .OrderBy(s => s.DisplayOrder)
            .ToListAsync();
    }

    public async Task<Staff?> GetByIdAsync(int id)
    {
        return await _context.Staff
            .Include(s => s.JobType)
            .Include(s => s.EmploymentType)
            .Include(s => s.Qualification)
            .Include(s => s.PermanentConstraints)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task AddAsync(Staff staff)
    {
        staff.CreatedAt = DateTime.UtcNow;
        staff.UpdatedAt = DateTime.UtcNow;
        _context.Staff.Add(staff);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Staff staff)
    {
        staff.UpdatedAt = DateTime.UtcNow;
        _context.Staff.Update(staff);
        await _context.SaveChangesAsync();
    }

    public async Task DeactivateAsync(int id)
    {
        var staff = await _context.Staff.FindAsync(id);
        if (staff != null)
        {
            staff.IsActive = false;
            staff.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}
