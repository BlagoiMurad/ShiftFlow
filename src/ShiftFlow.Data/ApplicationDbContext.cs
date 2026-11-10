using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ShiftFlow.Data.Entities;

namespace ShiftFlow.Data;

/// <summary>Слой за достъп до данни: контекст на Entity Framework Core.</summary>
public class ApplicationDbContext : IdentityDbContext<Employee>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<Shift> Shifts => Set<Shift>();
    public DbSet<ShiftSwapRequest> ShiftSwapRequests => Set<ShiftSwapRequest>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    public DbSet<TimeEntry> TimeEntries => Set<TimeEntry>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.Entity<Employee>(e =>
        {
            e.Property(x => x.FirstName).HasMaxLength(50).IsRequired();
            e.Property(x => x.LastName).HasMaxLength(50).IsRequired();
            e.Ignore(x => x.FullName);
            e.HasOne(x => x.Department).WithMany(d => d.Employees)
                .HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Position).WithMany(p => p.Employees)
                .HasForeignKey(x => x.PositionId).OnDelete(DeleteBehavior.SetNull);
        });

        b.Entity<Department>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.Name).IsUnique();
        });

        b.Entity<Position>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.HourlyRate).HasPrecision(10, 2);
        });

        b.Entity<Shift>(e =>
        {
            e.Ignore(x => x.PlannedHours);
            e.Property(x => x.Notes).HasMaxLength(500);
            e.HasOne(x => x.Employee).WithMany(x => x.Shifts)
                .HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Department).WithMany(x => x.Shifts)
                .HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.EmployeeId, x.Start });
        });

        b.Entity<ShiftSwapRequest>(e =>
        {
            e.Property(x => x.Reason).HasMaxLength(500);
            e.Property(x => x.ManagerComment).HasMaxLength(500);
            e.HasOne(x => x.Shift).WithMany().HasForeignKey(x => x.ShiftId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.TargetShift).WithMany().HasForeignKey(x => x.TargetShiftId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Requester).WithMany().HasForeignKey(x => x.RequesterId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.TargetEmployee).WithMany().HasForeignKey(x => x.TargetEmployeeId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ReviewedBy).WithMany().HasForeignKey(x => x.ReviewedById).OnDelete(DeleteBehavior.SetNull);
        });

        b.Entity<LeaveRequest>(e =>
        {
            e.Ignore(x => x.Days);
            e.Property(x => x.Reason).HasMaxLength(500);
            e.Property(x => x.ManagerComment).HasMaxLength(500);
            e.HasOne(x => x.Employee).WithMany(x => x.LeaveRequests)
                .HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.ReviewedBy).WithMany().HasForeignKey(x => x.ReviewedById).OnDelete(DeleteBehavior.SetNull);
        });

        b.Entity<TimeEntry>(e =>
        {
            e.Ignore(x => x.WorkedHours);
            e.Property(x => x.Note).HasMaxLength(300);
            e.HasOne(x => x.Employee).WithMany(x => x.TimeEntries)
                .HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Shift).WithMany(x => x.TimeEntries)
                .HasForeignKey(x => x.ShiftId).OnDelete(DeleteBehavior.SetNull);
        });
    }
}
