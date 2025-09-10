using Document_Management.Data;
using Document_Management.Repository;
using Document_Management.Service;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();

//New added middleware
builder.Services.AddDbContext<ApplicationDbContext>(options =>
options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure session services
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

//DI
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddScoped<UserRepo>();
builder.Services.AddScoped<ReportRepo>();
builder.Services.AddScoped<ICloudStorageService, GoogleCloudStorageService>();
builder.Services.AddScoped<CloudStorageMigrationService>();

// Configure for Cloud Run if deployed there
// if (builder.Environment.IsProduction())
// {
//     builder.WebHost.ConfigureKestrel(options =>
//     {
//         options.ListenAnyIP(8080); // Cloud Run requires port 8080
//     });
// }

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// This code is to change the behaviour of timestamp of postgresql
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

app.UseHttpsRedirection();
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