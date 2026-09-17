using Microsoft.EntityFrameworkCore;
using ShiftManagement.Core.Models;

namespace ShiftManagement.Data;

public class ShiftDbContext : DbContext
{
    public ShiftDbContext(DbContextOptions<ShiftDbContext> options) : base(options) { }

    public DbSet<FacilitySettings> FacilitySettings { get; set; } = null!;
    public DbSet<JobType> JobTypes { get; set; } = null!;
    public DbSet<EmploymentType> EmploymentTypes { get; set; } = null!;
    public DbSet<Qualification> Qualifications { get; set; } = null!;
    public DbSet<Staff> Staff { get; set; } = null!;
    public DbSet<ShiftType> ShiftTypes { get; set; } = null!;
    public DbSet<ShiftRequirement> ShiftRequirements { get; set; } = null!;
    public DbSet<MonthlySchedule> MonthlySchedules { get; set; } = null!;
    public DbSet<ScheduleCase> ScheduleCases { get; set; } = null!;
    public DbSet<ShiftAssignment> ShiftAssignments { get; set; } = null!;
    public DbSet<PersonalConstraint> PersonalConstraints { get; set; } = null!;
    public DbSet<StaffPermanentConstraint> StaffPermanentConstraints { get; set; } = null!;
    public DbSet<NightDutyTarget> NightDutyTargets { get; set; } = null!;
    public DbSet<StaffEvaluation> StaffEvaluations { get; set; } = null!;
    public DbSet<ValidationResultData> ValidationResults { get; set; } = null!;
    public DbSet<ConstraintChangeHistory> ConstraintChangeHistories { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // FacilitySettings
        modelBuilder.Entity<FacilitySettings>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.FacilityName).IsRequired();
        });

        // JobType
        modelBuilder.Entity<JobType>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Code).IsUnique();
        });

        // EmploymentType
        modelBuilder.Entity<EmploymentType>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Code).IsUnique();
        });

        // Qualification
        modelBuilder.Entity<Qualification>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Code).IsUnique();
        });

        // Staff
        modelBuilder.Entity<Staff>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.JobType).WithMany().HasForeignKey(x => x.JobTypeId);
            e.HasOne(x => x.EmploymentType).WithMany().HasForeignKey(x => x.EmploymentTypeId);
            e.HasOne(x => x.Qualification).WithMany().HasForeignKey(x => x.QualificationId);
            e.HasMany(x => x.PermanentConstraints).WithOne(x => x.Staff).HasForeignKey(x => x.StaffId);
            e.Ignore(x => x.IsNagaokaTaeko);
            e.Ignore(x => x.IsUematsuShinji);
            e.Ignore(x => x.IsMiyoshiTakaaki);
            e.Ignore(x => x.IsMatsuuraYoko);
            e.Ignore(x => x.IsToyofukuYumi);
        });

        // ShiftType
        modelBuilder.Entity<ShiftType>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Code).IsUnique();
            e.Ignore(x => x.IsDayOff);
            e.Ignore(x => x.IsNightRelated);
            e.Ignore(x => x.ShiftCategory);
        });

        // ShiftRequirement
        modelBuilder.Entity<ShiftRequirement>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.ShiftType).WithMany().HasForeignKey(x => x.ShiftTypeId);
        });

        // MonthlySchedule
        modelBuilder.Entity<MonthlySchedule>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.Year, x.Month }).IsUnique();
            e.HasMany(x => x.Cases).WithOne(x => x.MonthlySchedule).HasForeignKey(x => x.MonthlyScheduleId);
            e.HasMany(x => x.PersonalConstraints).WithMany();
            e.Ignore(x => x.DaysInMonth);
            e.Ignore(x => x.AllDates);
        });

        // ScheduleCase
        modelBuilder.Entity<ScheduleCase>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasMany(x => x.Assignments).WithOne().HasForeignKey(x => x.ScheduleCaseId);
            e.HasMany(x => x.StaffEvaluations).WithOne().HasForeignKey(x => x.ScheduleCaseId);
            e.HasOne(x => x.ValidationResult).WithOne().HasForeignKey<ValidationResultData>(x => x.ScheduleCaseId);
        });

        // ShiftAssignment
        modelBuilder.Entity<ShiftAssignment>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.ScheduleCaseId, x.StaffId, x.Date }).IsUnique();
            e.HasOne(x => x.Staff).WithMany().HasForeignKey(x => x.StaffId);
            e.HasOne(x => x.ShiftType).WithMany().HasForeignKey(x => x.ShiftTypeId);
            e.Property(x => x.Date).HasConversion(
                d => d.ToDateTime(TimeOnly.MinValue),
                dt => DateOnly.FromDateTime(dt));
        });

        // PersonalConstraint
        modelBuilder.Entity<PersonalConstraint>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Staff).WithMany().HasForeignKey(x => x.StaffId);
            e.HasOne(x => x.ShiftType).WithMany().HasForeignKey(x => x.ShiftTypeId);
            e.Property(x => x.TargetDate).HasConversion(
                d => d.HasValue ? d.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
                dt => dt.HasValue ? DateOnly.FromDateTime(dt.Value) : (DateOnly?)null);
        });

        // StaffPermanentConstraint
        modelBuilder.Entity<StaffPermanentConstraint>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Staff).WithMany(x => x.PermanentConstraints).HasForeignKey(x => x.StaffId);
            e.HasOne(x => x.ShiftType).WithMany().HasForeignKey(x => x.ShiftTypeId);
        });

        // NightDutyTarget
        modelBuilder.Entity<NightDutyTarget>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.MonthlyScheduleId, x.StaffId }).IsUnique();
        });

        // ValidationResultData
        modelBuilder.Entity<ValidationResultData>(e =>
        {
            e.HasKey(x => x.Id);
        });
    }
}
