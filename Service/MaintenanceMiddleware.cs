using Document_Management.Data;
using Document_Management.Utility.Constants;
using Microsoft.EntityFrameworkCore;

namespace Document_Management.Service;

public class MaintenanceMiddleware
{
    private readonly RequestDelegate _next;

    public MaintenanceMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IServiceProvider serviceProvider)
    {
        // Get the DbContext (or use your repository/unit of work)
        using (var scope = serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Check Maintenance Mode (update table/field as necessary)
            var isMaintenanceMode = await dbContext.AppSettings
                .Where(s => s.SettingKey == AppSettingKey.MaintenanceMode)
                .Select(s => s.Value == "true")
                .FirstOrDefaultAsync();

            if (isMaintenanceMode && !context.Request.Path.StartsWithSegments("/User/Home/Maintenance"))
            {
                context.Response.Redirect("/Home/Maintenance");
                return;
            }
        }

        await _next(context);
    }
}