using Document_Management.Services;
using Microsoft.AspNetCore.Mvc;

namespace Document_Management.Controllers
{
    public class MigrationController : Controller
    {
        private readonly CloudStorageMigrationService _migrationService;
        private readonly ILogger<MigrationController> _logger;

        public MigrationController(CloudStorageMigrationService migrationService, ILogger<MigrationController> logger)
        {
            _migrationService = migrationService;
            _logger = logger;
        }

        // GET: Migration
        public async Task<IActionResult> Index()
        {
            // Check if user is admin (add your authorization logic here)
            var userRole = HttpContext.Session.GetString("userrole")?.ToLower();
            if (userRole != "admin")
            {
                TempData["ErrorMessage"] = "Only administrators can access the migration tool.";
                return RedirectToAction("Privacy", "Home");
            }

            var pendingCount = await _migrationService.GetPendingMigrationCountAsync();
            ViewBag.PendingMigrationCount = pendingCount;
            
            return View();
        }

        // POST: Migration/StartMigration
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartMigration()
        {
            var userRole = HttpContext.Session.GetString("userrole")?.ToLower();
            if (userRole != "admin")
            {
                return Json(new { success = false, message = "Unauthorized access." });
            }

            try
            {
                var result = await _migrationService.MigrateAllFilesToCloudAsync();
                
                if (result.IsSuccess)
                {
                    return Json(new
                    {
                        success = true,
                        message = $"Migration completed successfully! {result.SuccessCount} files migrated.",
                        successCount = result.SuccessCount,
                        failureCount = result.FailureCount
                    });
                }
                else
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Migration completed with errors. Success: {result.SuccessCount}, Failed: {result.FailureCount}",
                        successCount = result.SuccessCount,
                        failureCount = result.FailureCount,
                        failedFiles = result.FailedFiles,
                        generalError = result.GeneralError
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Migration failed with exception");
                return Json(new
                {
                    success = false,
                    message = "Migration failed: " + ex.Message
                });
            }
        }

        // GET: Migration/Status
        public async Task<IActionResult> GetStatus()
        {
            var userRole = HttpContext.Session.GetString("userrole")?.ToLower();
            if (userRole != "admin")
            {
                return Json(new { success = false, message = "Unauthorized access." });
            }

            try
            {
                var pendingCount = await _migrationService.GetPendingMigrationCountAsync();
                return Json(new
                {
                    success = true,
                    pendingCount = pendingCount
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }
    }
}