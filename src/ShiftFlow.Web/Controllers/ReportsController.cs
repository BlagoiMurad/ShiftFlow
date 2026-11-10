using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ShiftFlow.Services.Common;
using ShiftFlow.Services.Interfaces;
using ShiftFlow.Web.Models;

namespace ShiftFlow.Web.Controllers;

/// <summary>Справки: планирани срещу реално отработени часове.</summary>
[Authorize(Roles = Roles.AdminOrManager)]
public class ReportsController : Controller
{
    private readonly IReportService _reports;
    private readonly IDepartmentService _departments;

    public ReportsController(IReportService reports, IDepartmentService departments)
    {
        _reports = reports;
        _departments = departments;
    }

    public async Task<IActionResult> Index(DateTime? from, DateTime? to, int? departmentId)
    {
        var start = from ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var end = to ?? start.AddMonths(1).AddDays(-1);
        return View(new ReportViewModel
        {
            From = start,
            To = end,
            DepartmentId = departmentId,
            Departments = new SelectList(await _departments.GetAllAsync(), "Id", "Name", departmentId),
            Rows = await _reports.GetHoursReportAsync(start, end.AddDays(1), departmentId)
        });
    }

    /// <summary>Експорт на справката в CSV (отваря се с Excel).</summary>
    public async Task<IActionResult> ExportCsv(DateTime from, DateTime to, int? departmentId)
    {
        var rows = await _reports.GetHoursReportAsync(from, to.AddDays(1), departmentId);
        var inv = CultureInfo.InvariantCulture;
        var sb = new StringBuilder();
        sb.AppendLine("Служител;Отдел;Смени;Планирани часове;Отработени часове;Разлика;Закъснения;Дни отпуск;Заплащане (лв.)");
        foreach (var r in rows)
        {
            sb.AppendLine(string.Join(";", r.EmployeeName, r.DepartmentName ?? "",
                r.ShiftCount, r.PlannedHours.ToString("0.##", inv), r.WorkedHours.ToString("0.##", inv),
                r.Difference.ToString("0.##", inv), r.LateArrivals, r.LeaveDays, r.EstimatedPay.ToString("0.00", inv)));
        }
        // UTF-8 с BOM, за да се показва кирилицата правилно в Excel.
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv", $"spravka_{from:yyyyMMdd}_{to:yyyyMMdd}.csv");
    }
}
