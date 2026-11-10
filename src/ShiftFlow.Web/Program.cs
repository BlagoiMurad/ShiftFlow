using System.Globalization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using ShiftFlow.Data;
using ShiftFlow.Data.Entities;
using ShiftFlow.Services;
using ShiftFlow.Web.Data;

var builder = WebApplication.CreateBuilder(args);

// Слой за достъп до данни
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Потребители и роли (ASP.NET Core Identity)
builder.Services.AddIdentity<Employee, IdentityRole>(options =>
    {
        options.Password.RequiredLength = 6;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

// Бизнес слой
builder.Services.AddShiftFlowServices();

// Потребителски интерфейс
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Български формат за дати, с точка за десетични числа (за полетата type="number").
var culture = (CultureInfo)CultureInfo.GetCultureInfo("bg-BG").Clone();
culture.NumberFormat.NumberDecimalSeparator = ".";
culture.NumberFormat.CurrencyDecimalSeparator = ".";
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(culture),
    SupportedCultures = new[] { culture },
    SupportedUICultures = new[] { culture }
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Създаване на базата и начални данни
await DbSeeder.SeedAsync(app.Services, app.Environment.IsDevelopment(), app.Configuration);

app.Run();
