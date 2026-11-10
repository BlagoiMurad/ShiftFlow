using Microsoft.Extensions.DependencyInjection;
using ShiftFlow.Services.Implementations;
using ShiftFlow.Services.Interfaces;

namespace ShiftFlow.Services;

public static class ServiceCollectionExtensions
{
    /// <summary>Регистрира всички услуги от бизнес слоя.</summary>
    public static IServiceCollection AddShiftFlowServices(this IServiceCollection services)
    {
        services.AddScoped<IDepartmentService, DepartmentService>();
        services.AddScoped<IPositionService, PositionService>();
        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<IShiftService, ShiftService>();
        services.AddScoped<ISwapRequestService, SwapRequestService>();
        services.AddScoped<ILeaveService, LeaveService>();
        services.AddScoped<ITimeTrackingService, TimeTrackingService>();
        services.AddScoped<IReportService, ReportService>();
        return services;
    }
}
