using Document_Management.Data;
using Document_Management.Repository;
using Document_Management.Service;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();

//// Load configuration based on the environment
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

//New added middleware
builder.Services.AddDbContext<ApplicationDbContext>(options =>
options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure session services
builder.Services.AddDistributedMemoryCache();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;
    options.LoginPath = $"/Identity/Account/Login";
    options.LogoutPath = $"/Identity/Account/Logout";
    options.AccessDeniedPath = $"/Identity/Account/AccessDenied";
});

//DI
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddScoped<UserRepo>();
builder.Services.AddScoped<ReportRepo>();
builder.Services.AddScoped<ICloudStorageService, GoogleCloudStorageService>();
builder.Services.AddScoped<CloudStorageMigrationService>();

if (builder.Environment.IsProduction())
{
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ListenAnyIP(8080);
    });
}

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.MapGet("/health", () => Results.Ok("Healthy"));

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

app.UseMiddleware<MaintenanceMiddleware>();

app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

// Additional routes for your DMS controller
app.MapControllerRoute(
    name: "dmsRoutes",
    pattern: "Dms/{action=Index}/{id?}",
    defaults: new { controller = "Dms" });

// Route for file downloads
app.MapControllerRoute(
    name: "fileDownload",
    pattern: "Dms/Download/{*filepath}",
    defaults: new { controller = "Dms", action = "Download" });

// Route for displaying files with complex parameters
app.MapControllerRoute(
    name: "displayFiles",
    pattern: "Dms/DisplayFiles/{companyFolderName}/{yearFolderName}/{departmentFolderName}/{documentTypeFolderName}/{subCategoryFolder?}",
    defaults: new { controller = "Dms", action = "DisplayFiles" });

app.Run();