using ShiftManagement.Core.Models;

namespace ShiftManagement.Core.Interfaces;

public interface IStaffRepository
{
    Task<List<Staff>> GetAllActiveAsync();
    Task<Staff?> GetByIdAsync(int id);
    Task AddAsync(Staff staff);
    Task UpdateAsync(Staff staff);
    Task DeactivateAsync(int id);
}

public interface IScheduleRepository
{
    Task<MonthlySchedule?> GetByYearMonthAsync(int year, int month);
    Task<MonthlySchedule> CreateOrGetAsync(int year, int month);
    Task SaveCaseAsync(ScheduleCase scheduleCase);
    Task<List<MonthlySchedule>> GetAllAsync();
    Task UpdateStatusAsync(int scheduleId, ScheduleStatus status);
}

public interface IPersonalConstraintRepository
{
    Task<List<PersonalConstraint>> GetByScheduleAsync(int scheduleId);
    Task<List<PersonalConstraint>> GetByStaffAsync(int scheduleId, int staffId);
    Task SaveAsync(PersonalConstraint constraint);
    Task DeleteAsync(int id);
    Task BulkSaveAsync(IEnumerable<PersonalConstraint> constraints);
}

public interface IFacilitySettingsRepository
{
    Task<FacilitySettings> GetAsync();
    Task SaveAsync(FacilitySettings settings);
}

public interface IShiftTypeRepository
{
    Task<List<ShiftType>> GetAllAsync();
    Task<ShiftType?> GetByCodeAsync(string code);
    Task SaveAsync(ShiftType shiftType);
}
