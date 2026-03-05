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

        // Display the form
        public IActionResult ActivityReportForm()
        {
            return View(new ActivityReportViewModel());
        }

        // Generate the report
        public async Task<IActionResult> GenerateFileUploadReport(DateOnly dateFrom, DateOnly dateTo)
        {
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