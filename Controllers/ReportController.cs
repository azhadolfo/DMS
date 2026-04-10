using Document_Management.Models;
using Document_Management.Repository;
using Microsoft.AspNetCore.Mvc;

namespace Document_Management.Controllers
{
    public class ReportController : Controller
    {
        private readonly ReportRepo _reportRepo;
        private readonly ILogger<ReportController> _logger;

        public ReportController(ReportRepo reportRepo, ILogger<ReportController> logger)
        {
            _reportRepo = reportRepo;
            _logger = logger;
        }

        // Display the form
        public IActionResult ActivityReportForm()
        {
            return View(new ActivityReportViewModel());
        }

        // Generate the report
        public async Task<IActionResult> GenerateFileUploadReport(DateOnly dateFrom, DateOnly dateTo)
        {
            try
            {
                if (dateFrom > dateTo)
                {
                    TempData["error"] = "Date from cannot be greater than Date to date.";
                    return RedirectToAction(nameof(ActivityReportForm));
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate file upload report from {DateFrom} to {DateTo}.", dateFrom, dateTo);
                TempData["ErrorMessage"] = "Failed to generate report.";
                return RedirectToAction(nameof(ActivityReportForm));
            }
        }
    }
}
