using Microsoft.EntityFrameworkCore;
using ShiftManagement.Core.Interfaces;
using ShiftManagement.Core.Models;

namespace ShiftManagement.Data.Repositories;

public class ShiftTypeRepository : IShiftTypeRepository
{
    private readonly ShiftDbContext _context;

    public ShiftTypeRepository(ShiftDbContext context)
    {
        _context = context;
    }

    public async Task<List<ShiftType>> GetAllAsync()
    {
        return await _context.ShiftTypes
            .OrderBy(s => s.DisplayOrder)
            .ToListAsync();
    }

    public async Task<ShiftType?> GetByCodeAsync(string code)
    {
        return await _context.ShiftTypes
            .FirstOrDefaultAsync(s => s.Code == code);
    }

    public async Task SaveAsync(ShiftType shiftType)
    {
        if (shiftType.Id == 0)
        {
            _context.ShiftTypes.Add(shiftType);
        }
        else
        {
            _context.ShiftTypes.Update(shiftType);
        }
        await _context.SaveChangesAsync();
    }
}
