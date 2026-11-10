using Microsoft.EntityFrameworkCore;
using ShiftFlow.Data;
using ShiftFlow.Data.Entities;
using ShiftFlow.Data.Enums;
using ShiftFlow.Services.Common;
using ShiftFlow.Services.Interfaces;

namespace ShiftFlow.Services.Implementations;

/// <summary>
/// Размяна на смени в три стъпки:
/// 1) служителят създава заявка към колега; 2) колегата приема или отказва;
/// 3) управителят одобрява и смените се разменят (или смяната се прехвърля).
/// </summary>
public class SwapRequestService : ISwapRequestService
{
    private readonly ApplicationDbContext _db;
    private readonly IShiftService _shifts;

    public SwapRequestService(ApplicationDbContext db, IShiftService shifts)
    {
        _db = db;
        _shifts = shifts;
    }

    private IQueryable<ShiftSwapRequest> Query() => _db.ShiftSwapRequests
        .Include(r => r.Shift).ThenInclude(s => s.Department)
        .Include(r => r.TargetShift)
        .Include(r => r.Requester)
        .Include(r => r.TargetEmployee)
        .Include(r => r.ReviewedBy);

    public Task<List<ShiftSwapRequest>> GetForEmployeeAsync(string employeeId) =>
        Query().Where(r => r.RequesterId == employeeId || r.TargetEmployeeId == employeeId)
            .OrderByDescending(r => r.CreatedAt).ToListAsync();

    public Task<List<ShiftSwapRequest>> GetPendingForManagerAsync() =>
        Query().Where(r => r.Status == RequestStatus.PendingManager)
            .OrderBy(r => r.CreatedAt).ToListAsync();

    public Task<List<ShiftSwapRequest>> GetAllAsync() =>
        Query().OrderByDescending(r => r.CreatedAt).ToListAsync();

    public Task<ShiftSwapRequest?> GetByIdAsync(int id) => Query().FirstOrDefaultAsync(r => r.Id == id);

    public async Task<ServiceResult> CreateAsync(string requesterId, int shiftId, string targetEmployeeId, int? targetShiftId, string? reason)
    {
        var shift = await _db.Shifts.FindAsync(shiftId);
        if (shift == null || shift.EmployeeId != requesterId)
            return ServiceResult.Fail("Можете да предлагате за размяна само свои смени.");
        if (shift.Status != ShiftStatus.Planned || shift.Start <= DateTime.Now)
            return ServiceResult.Fail("Могат да се разменят само предстоящи планирани смени.");
        if (targetEmployeeId == requesterId)
            return ServiceResult.Fail("Изберете друг колега.");

        var target = await _db.Users.FindAsync(targetEmployeeId);
        if (target == null || !target.IsActive)
            return ServiceResult.Fail("Колегата не е намерен.");

        if (targetShiftId.HasValue)
        {
            var targetShift = await _db.Shifts.FindAsync(targetShiftId.Value);
            if (targetShift == null || targetShift.EmployeeId != targetEmployeeId)
                return ServiceResult.Fail("Избраната смяна не е на този колега.");
            if (targetShift.Status != ShiftStatus.Planned || targetShift.Start <= DateTime.Now)
                return ServiceResult.Fail("Смяната на колегата трябва да е предстояща.");
        }

        bool alreadyPending = await _db.ShiftSwapRequests.AnyAsync(r => r.ShiftId == shiftId &&
            (r.Status == RequestStatus.PendingColleague || r.Status == RequestStatus.PendingManager));
        if (alreadyPending)
            return ServiceResult.Fail("За тази смяна вече има активна заявка.");

        _db.ShiftSwapRequests.Add(new ShiftSwapRequest
        {
            ShiftId = shiftId,
            RequesterId = requesterId,
            TargetEmployeeId = targetEmployeeId,
            TargetShiftId = targetShiftId,
            Reason = reason,
            Status = RequestStatus.PendingColleague,
            CreatedAt = DateTime.Now
        });
        await _db.SaveChangesAsync();
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> RespondAsColleagueAsync(int requestId, string employeeId, bool accept)
    {
        var request = await _db.ShiftSwapRequests.FindAsync(requestId);
        if (request == null || request.TargetEmployeeId != employeeId)
            return ServiceResult.Fail("Заявката не е намерена.");
        if (request.Status != RequestStatus.PendingColleague)
            return ServiceResult.Fail("На заявката вече е отговорено.");

        request.Status = accept ? RequestStatus.PendingManager : RequestStatus.Declined;
        await _db.SaveChangesAsync();
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> ReviewAsync(int requestId, string managerId, bool approve, string? comment)
    {
        var request = await _db.ShiftSwapRequests
            .Include(r => r.Shift).Include(r => r.TargetShift)
            .FirstOrDefaultAsync(r => r.Id == requestId);
        if (request == null) return ServiceResult.Fail("Заявката не е намерена.");
        if (request.Status != RequestStatus.PendingManager)
            return ServiceResult.Fail("Заявката не чака одобрение.");

        if (approve)
        {
            var shift = request.Shift;
            var targetShift = request.TargetShift;
            if (shift.EmployeeId != request.RequesterId || (targetShift != null && targetShift.EmployeeId != request.TargetEmployeeId))
                return ServiceResult.Fail("Смените са променени след създаването на заявката.");

            var ignore = new List<int> { shift.Id };
            if (targetShift != null) ignore.Add(targetShift.Id);

            // Проверка дали след размяната правилата се спазват за двамата служители.
            var check1 = new Shift { Id = shift.Id, EmployeeId = request.TargetEmployeeId, Start = shift.Start, End = shift.End, BreakMinutes = shift.BreakMinutes };
            var error = await _shifts.ValidateAsync(check1, ignore);
            if (error == null && targetShift != null)
            {
                var check2 = new Shift { Id = targetShift.Id, EmployeeId = request.RequesterId, Start = targetShift.Start, End = targetShift.End, BreakMinutes = targetShift.BreakMinutes };
                error = await _shifts.ValidateAsync(check2, ignore);
            }
            if (error != null) return ServiceResult.Fail("Размяната не може да бъде одобрена: " + error);

            shift.EmployeeId = request.TargetEmployeeId;
            if (targetShift != null) targetShift.EmployeeId = request.RequesterId;
            request.Status = RequestStatus.Approved;
        }
        else
        {
            request.Status = RequestStatus.Rejected;
        }

        request.ReviewedById = managerId;
        request.ReviewedAt = DateTime.Now;
        request.ManagerComment = comment;
        await _db.SaveChangesAsync();
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> CancelAsync(int requestId, string requesterId)
    {
        var request = await _db.ShiftSwapRequests.FindAsync(requestId);
        if (request == null || request.RequesterId != requesterId)
            return ServiceResult.Fail("Заявката не е намерена.");
        if (request.Status is not (RequestStatus.PendingColleague or RequestStatus.PendingManager))
            return ServiceResult.Fail("Заявката вече е обработена.");
        request.Status = RequestStatus.Cancelled;
        await _db.SaveChangesAsync();
        return ServiceResult.Ok();
    }
}
