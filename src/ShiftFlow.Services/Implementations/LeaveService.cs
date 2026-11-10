using Microsoft.EntityFrameworkCore;
using ShiftFlow.Data;
using ShiftFlow.Data.Entities;
using ShiftFlow.Data.Enums;
using ShiftFlow.Services.Common;
using ShiftFlow.Services.Interfaces;

namespace ShiftFlow.Services.Implementations;

public class LeaveService : ILeaveService
{
    /// <summary>Минимален годишен платен отпуск по Кодекса на труда.</summary>
    public const int AnnualPaidLeaveDays = 20;

    private readonly ApplicationDbContext _db;
    public LeaveService(ApplicationDbContext db) => _db = db;

    private IQueryable<LeaveRequest> Query() =>
        _db.LeaveRequests.Include(l => l.Employee).ThenInclude(e => e.Department).Include(l => l.ReviewedBy);

    public Task<List<LeaveRequest>> GetForEmployeeAsync(string employeeId) =>
        Query().Where(l => l.EmployeeId == employeeId).OrderByDescending(l => l.StartDate).ToListAsync();

    public Task<List<LeaveRequest>> GetAllAsync(RequestStatus? status = null)
    {
        var q = Query();
        if (status.HasValue) q = q.Where(l => l.Status == status);
        return q.OrderByDescending(l => l.CreatedAt).ToListAsync();
    }

    public Task<LeaveRequest?> GetByIdAsync(int id) => Query().FirstOrDefaultAsync(l => l.Id == id);

    public async Task<int> GetUsedPaidDaysAsync(string employeeId, int year)
    {
        var leaves = await _db.LeaveRequests
            .Where(l => l.EmployeeId == employeeId && l.Type == LeaveType.Paid &&
                        l.Status == RequestStatus.Approved && l.StartDate.Year == year)
            .ToListAsync();
        return leaves.Sum(l => l.Days);
    }

    /// <summary>Платени дни за годината, които са одобрени или чакат одобрение (запазени).</summary>
    private async Task<int> GetReservedPaidDaysAsync(string employeeId, int year, int? excludeRequestId = null)
    {
        var leaves = await _db.LeaveRequests
            .Where(l => l.EmployeeId == employeeId && l.Type == LeaveType.Paid &&
                        (l.Status == RequestStatus.Approved || l.Status == RequestStatus.PendingManager) &&
                        l.StartDate.Year == year && l.Id != excludeRequestId)
            .ToListAsync();
        return leaves.Sum(l => l.Days);
    }

    public async Task<ServiceResult> CreateAsync(LeaveRequest request)
    {
        request.StartDate = request.StartDate.Date;
        request.EndDate = request.EndDate.Date;

        if (request.EndDate < request.StartDate)
            return ServiceResult.Fail("Крайната дата трябва да е след началната.");
        if (request.Type != LeaveType.Sick && request.StartDate < DateTime.Today)
            return ServiceResult.Fail("Не може да се иска отпуск за минали дати.");

        bool overlaps = await _db.LeaveRequests.AnyAsync(l => l.EmployeeId == request.EmployeeId &&
            (l.Status == RequestStatus.PendingManager || l.Status == RequestStatus.Approved) &&
            l.StartDate <= request.EndDate && l.EndDate >= request.StartDate);
        if (overlaps)
            return ServiceResult.Fail("Вече имате заявка за отпуск, която се застъпва с тези дати.");

        if (request.Type == LeaveType.Paid)
        {
            // Чакащите заявки също се броят, за да не се надхвърли лимитът с няколко заявки наведнъж.
            int reserved = await GetReservedPaidDaysAsync(request.EmployeeId, request.StartDate.Year);
            if (reserved + request.Days > AnnualPaidLeaveDays)
                return ServiceResult.Fail($"Остават ви {Math.Max(0, AnnualPaidLeaveDays - reserved)} дни платен отпуск за годината (с чакащите заявки).");
        }

        request.Status = RequestStatus.PendingManager;
        request.CreatedAt = DateTime.Now;
        _db.LeaveRequests.Add(request);
        await _db.SaveChangesAsync();
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> ReviewAsync(int requestId, string managerId, bool approve, string? comment)
    {
        var request = await _db.LeaveRequests.FindAsync(requestId);
        if (request == null) return ServiceResult.Fail("Заявката не е намерена.");
        if (request.Status != RequestStatus.PendingManager)
            return ServiceResult.Fail("Заявката вече е обработена.");

        if (approve && request.Type == LeaveType.Paid)
        {
            int used = await GetUsedPaidDaysAsync(request.EmployeeId, request.StartDate.Year);
            if (used + request.Days > AnnualPaidLeaveDays)
                return ServiceResult.Fail($"Служителят има само {Math.Max(0, AnnualPaidLeaveDays - used)} оставащи дни платен отпуск.");
        }

        request.Status = approve ? RequestStatus.Approved : RequestStatus.Rejected;
        request.ReviewedById = managerId;
        request.ReviewedAt = DateTime.Now;
        request.ManagerComment = comment;

        if (approve)
        {
            // Смените на служителя по време на отпуска се отменят автоматично.
            var from = request.StartDate.Date;
            var to = request.EndDate.Date.AddDays(1);
            var shifts = await _db.Shifts.Where(s => s.EmployeeId == request.EmployeeId &&
                s.Status == ShiftStatus.Planned && s.Start < to && s.End > from).ToListAsync();
            foreach (var s in shifts)
            {
                s.Status = ShiftStatus.Cancelled;
                s.Notes = (s.Notes + " [Отменена поради отпуск]").Trim();
            }
        }

        await _db.SaveChangesAsync();
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> CancelAsync(int requestId, string employeeId)
    {
        var request = await _db.LeaveRequests.FindAsync(requestId);
        if (request == null || request.EmployeeId != employeeId)
            return ServiceResult.Fail("Заявката не е намерена.");
        if (request.Status != RequestStatus.PendingManager)
            return ServiceResult.Fail("Може да отказвате само чакащи заявки.");
        request.Status = RequestStatus.Cancelled;
        await _db.SaveChangesAsync();
        return ServiceResult.Ok();
    }
}
