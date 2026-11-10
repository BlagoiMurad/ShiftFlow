using ShiftFlow.Data.Entities;
using ShiftFlow.Data.Enums;
using ShiftFlow.Services.Common;

namespace ShiftFlow.Services.Interfaces;

public interface IDepartmentService
{
    Task<List<Department>> GetAllAsync();
    Task<Department?> GetByIdAsync(int id);
    Task<ServiceResult> CreateAsync(Department department);
    Task<ServiceResult> UpdateAsync(Department department);
    Task<ServiceResult> DeleteAsync(int id);
}

public interface IPositionService
{
    Task<List<Position>> GetAllAsync();
    Task<Position?> GetByIdAsync(int id);
    Task<ServiceResult> CreateAsync(Position position);
    Task<ServiceResult> UpdateAsync(Position position);
    Task<ServiceResult> DeleteAsync(int id);
}

public interface IEmployeeService
{
    Task<List<Employee>> GetAllAsync(int? departmentId = null, bool onlyActive = false);
    Task<Employee?> GetByIdAsync(string id);
    Task<string?> GetRoleAsync(Employee employee);
    Task<ServiceResult<string>> CreateAsync(Employee employee, string password, string role);
    Task<ServiceResult> UpdateAsync(Employee employee, string role);
    Task<ServiceResult> SetActiveAsync(string id, bool isActive);
}

public interface IShiftService
{
    Task<List<Shift>> GetForPeriodAsync(DateTime from, DateTime to, int? departmentId = null, string? employeeId = null);
    Task<Shift?> GetByIdAsync(int id);
    Task<ServiceResult> CreateAsync(Shift shift);
    Task<ServiceResult> UpdateAsync(Shift shift);
    Task<ServiceResult> DeleteAsync(int id);
    /// <summary>Копира всички смени от една седмица в следващата (бързо планиране).</summary>
    Task<ServiceResult<int>> CopyWeekAsync(DateTime sourceWeekStart, int? departmentId);
    /// <summary>Проверява смяната спрямо бизнес правилата. Връща грешка или null.</summary>
    Task<string?> ValidateAsync(Shift shift, IEnumerable<int>? ignoreShiftIds = null);
}

public interface ISwapRequestService
{
    Task<List<ShiftSwapRequest>> GetForEmployeeAsync(string employeeId);
    Task<List<ShiftSwapRequest>> GetPendingForManagerAsync();
    Task<List<ShiftSwapRequest>> GetAllAsync();
    Task<ShiftSwapRequest?> GetByIdAsync(int id);
    Task<ServiceResult> CreateAsync(string requesterId, int shiftId, string targetEmployeeId, int? targetShiftId, string? reason);
    Task<ServiceResult> RespondAsColleagueAsync(int requestId, string employeeId, bool accept);
    Task<ServiceResult> ReviewAsync(int requestId, string managerId, bool approve, string? comment);
    Task<ServiceResult> CancelAsync(int requestId, string requesterId);
}

public interface ILeaveService
{
    Task<List<LeaveRequest>> GetForEmployeeAsync(string employeeId);
    Task<List<LeaveRequest>> GetAllAsync(RequestStatus? status = null);
    Task<LeaveRequest?> GetByIdAsync(int id);
    Task<ServiceResult> CreateAsync(LeaveRequest request);
    Task<ServiceResult> ReviewAsync(int requestId, string managerId, bool approve, string? comment);
    Task<ServiceResult> CancelAsync(int requestId, string employeeId);
    /// <summary>Колко дни платен отпуск е използвал служителят през годината.</summary>
    Task<int> GetUsedPaidDaysAsync(string employeeId, int year);
}

public interface ITimeTrackingService
{
    Task<TimeEntry?> GetOpenEntryAsync(string employeeId);
    Task<ServiceResult<TimeEntry>> ClockInAsync(string employeeId, DateTime? now = null);
    Task<ServiceResult<TimeEntry>> ClockOutAsync(string employeeId, string? note, DateTime? now = null);
    Task<List<TimeEntry>> GetEntriesAsync(DateTime from, DateTime to, string? employeeId = null, int? departmentId = null);
    Task<TimeEntry?> GetByIdAsync(int id);
    Task<ServiceResult> SaveCorrectionAsync(TimeEntry entry);
    Task<ServiceResult> DeleteAsync(int id);
}

public interface IReportService
{
    Task<List<ReportRow>> GetHoursReportAsync(DateTime from, DateTime to, int? departmentId = null);
    Task<DashboardStats> GetDashboardStatsAsync();
}
