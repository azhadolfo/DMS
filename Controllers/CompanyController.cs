using Document_Management.Data;
using Document_Management.Models;
using Document_Management.Utility.Extensions;
using Document_Management.Utility.Helper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Document_Management.Controllers
{
    public class CompanyController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly string? _userRole;
        private readonly string? _userName;

        public CompanyController(ApplicationDbContext dbContext, IHttpContextAccessor httpContextAccessor)
        {
            _dbContext = dbContext;

            if (httpContextAccessor.HttpContext != null)
            {
                _userRole = httpContextAccessor.HttpContext.Session.GetString("userRole")?.ToLower();
                _userName = httpContextAccessor.HttpContext.Session.GetString("username");
            }
            else
            {
                _userRole = null;
            }
        }

        public async Task<IActionResult> Index()
        {
            var adminAccessResult = EnsureAdminAccess();
            if (adminAccessResult != null)
            {
                return adminAccessResult;
            }

            var companies = await _dbContext.Companies
                .OrderBy(u => u.CompanyName)
                .ToListAsync();

            return View(companies);
        }

        [HttpGet]
        public IActionResult Create()
        {
            var adminAccessResult = EnsureAdminAccess();
            if (adminAccessResult != null)
            {
                return adminAccessResult;
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CompanyViewModel viewModel, CancellationToken cancellationToken)
        {
            var adminAccessResult = EnsureAdminAccess();
            if (adminAccessResult != null)
            {
                return adminAccessResult;
            }

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "The information you submitted is not valid.";
                return View(viewModel);
            }

            var companyAlreadyExist = await _dbContext.Companies
                .AnyAsync(u => u.CompanyName == viewModel.CompanyName, cancellationToken);

            if (companyAlreadyExist)
            {
                ModelState.AddModelError("CompanyName", "The company with the same name already exists.");
                TempData["ErrorMessage"] = "The company with the same name already exists.";
                return View(viewModel);
            }

            var company = new Company
            {
                CompanyName = viewModel.CompanyName.RemoveCommas(),
                CreatedBy = _userName!,
            };

            await _dbContext.Companies.AddAsync(company, cancellationToken);

            LogsModel logs = new(_userName!, $"Add new company: {viewModel.CompanyName}");
            await _dbContext.Logs.AddAsync(logs, cancellationToken);

            await _dbContext.SaveChangesAsync(cancellationToken);
            TempData["success"] = "Company created successfully";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            var adminAccessResult = EnsureAdminAccess();
            if (adminAccessResult != null)
            {
                return adminAccessResult;
            }

            var company = await _dbContext.Companies
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (company == null)
            {
                return NotFound();
            }

            var viewModel = new CompanyViewModel
            {
                Id = company.Id,
                CompanyName = company.CompanyName.RemoveCommas(),
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CompanyViewModel viewModel, CancellationToken cancellationToken)
        {
            var adminAccessResult = EnsureAdminAccess();
            if (adminAccessResult != null)
            {
                return adminAccessResult;
            }

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "The information you submitted is not valid.";
                return View(viewModel);
            }

            var existingCompany = await _dbContext.Companies
                .FirstOrDefaultAsync(x => x.Id == viewModel.Id, cancellationToken);

            if (existingCompany == null)
            {
                return NotFound();
            }

            var companyAlreadyExist = await _dbContext.Companies
                .AnyAsync(u =>
                    u.Id != viewModel.Id &&
                    u.CompanyName == viewModel.CompanyName, cancellationToken);

            if (companyAlreadyExist)
            {
                ModelState.AddModelError("CompanyName", "The company with the same name already exists.");
                TempData["ErrorMessage"] = "The company with the same name already exists.";
                return View(viewModel);
            }

            var existingName = existingCompany.CompanyName;
            existingCompany.CompanyName = viewModel.CompanyName;
            existingCompany.EditedBy = _userName;
            existingCompany.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();

            LogsModel logs = new(_userName!, $"Update company from {existingName} to {viewModel.CompanyName}");
            await _dbContext.Logs.AddAsync(logs, cancellationToken);

            await _dbContext.SaveChangesAsync(cancellationToken);
            TempData["success"] = "Company updated successfully";
            return RedirectToAction("Index");
        }

        private IActionResult? EnsureAdminAccess()
        {
            if (string.IsNullOrEmpty(_userName))
            {
                return RedirectToAction("Login", "Account");
            }

            if (_userRole != "admin")
            {
                TempData["ErrorMessage"] = "You have no access to this action. Please contact the MIS Department if you think this is a mistake.";
                return RedirectToAction("Privacy", "Home");
            }

            return null;
        }
    }
}
