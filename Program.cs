using Document_Management.Data;
using Document_Management.Repository;
using Document_Management.Service;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Load configuration
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// MVC
builder.Services.AddControllersWithViews();

// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Session + Cookies
builder.Services.AddDistributedMemoryCache();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
});

// DI
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddScoped<UserRepo>();
builder.Services.AddScoped<ReportRepo>();
builder.Services.AddScoped<ICloudStorageService, GoogleCloudStorageService>();
builder.Services.AddScoped<CloudStorageMigrationService>();

var app = builder.Build();


// Health check (Cloud Run)
app.MapGet("/health", () => Results.Ok("Healthy"));

// HSTS ONLY in browser environment
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// PostgreSQL compatibility switch
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// Static files FIRST (same as your working project)
app.UseStaticFiles();

// Maintenance middleware
app.UseMiddleware<MaintenanceMiddleware>();

// Routing
app.UseRouting();

// Session (same order as your working project)
app.UseSession();

// Authentication (if you add identity later)
app.UseAuthentication();

// Authorization
app.UseAuthorization();

// Default route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

// DMS routes
app.MapControllerRoute(
    name: "dmsRoutes",
    pattern: "Dms/{action=Index}/{id?}",
    defaults: new { controller = "Dms" });

app.MapControllerRoute(
    name: "fileDownload",
    pattern: "Dms/Download/{*filepath}",
    defaults: new { controller = "Dms", action = "Download" });

app.MapControllerRoute(
    name: "displayFiles",
    pattern: "Dms/DisplayFiles/{companyFolderName}/{yearFolderName}/{departmentFolderName}/{documentTypeFolderName}/{subCategoryFolder?}",
    defaults: new { controller = "Dms", action = "DisplayFiles" });

app.Run();
