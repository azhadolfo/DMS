using Document_Management.Models;
using Document_Management.Repository;
using Microsoft.AspNetCore.Mvc;

namespace Document_Management.Controllers
{
    public class ReportController : Controller
    {
        private readonly ReportRepo _reportRepo;

        public ReportController(ReportRepo reportRepo)
        {
            _reportRepo = reportRepo;
        }

        public IActionResult? CheckAccess()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("username")))
            {
                return RedirectToAction("Login", "Account");
            }

            var userRole = HttpContext.Session.GetString("userrole")?.ToLower();
            var userModuleAccess = HttpContext.Session.GetString("usermoduleaccess");
            var userAccess = !string.IsNullOrEmpty(userModuleAccess) ? userModuleAccess.Split(',') : new string[0];

            if (userRole == "admin" || userAccess.Any(module => module.Trim() == "DMS"))
            {
                return null;
            }
            
            TempData["ErrorMessage"] = "You have no access to this action. Please contact the MIS Department if you think this is a mistake.";
            return RedirectToAction("Privacy", "Home");

        }

        // Display the form
        public IActionResult ActivityReportForm()
        {
            var accessCheckResult = CheckAccess();
            return accessCheckResult ?? View(new ActivityReportViewModel());
        }

        // Generate the report
        public async Task<IActionResult> GenerateFileUploadReport(DateOnly dateFrom, DateOnly dateTo)
        {
            var accessCheckResult = CheckAccess();
            if (accessCheckResult != null)
            {
                return accessCheckResult;
            }

            var uploadedFiles = await _reportRepo.GenerateUploadedFiles(dateFrom, dateTo);

            var model = new ActivityReportViewModel
            {
                DateFrom = dateFrom,
                DateTo = dateTo,
                UploadedFiles = uploadedFiles,
                CurrentUser = HttpContext.Session.GetString("username") ?? string.Empty
            };

            return View("FileUploadReport", model);
        }
    }
}
