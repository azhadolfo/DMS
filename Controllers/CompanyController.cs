using Document_Management.Data;
using Document_Management.Models;
using Document_Management.Utility.Helper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Document_Management.Controllers;

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
            _userRole = httpContextAccessor.HttpContext.Session.GetString("userrole")?.ToLower();
            _userName = httpContextAccessor.HttpContext.Session.GetString("username");
        }
        else
        {
            _userRole = null;
        }
    }
    
    public async Task<IActionResult> Index()
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

        var companies = await _dbContext.Companies
            .OrderBy(u => u.CompanyName)
            .ToListAsync();

        return View(companies);
    }
    
    [HttpGet]
    public IActionResult Create()
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
            
        return View();
        
    }

    [HttpPost]
    public async Task<IActionResult> Create(CompanyViewModel viewModel, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "The information you submitted is not valid.";
            return View(viewModel);
        }
        
        var companyAlreadyExist = await _dbContext.Companies
            .AnyAsync(u => u.CompanyName == viewModel.CompanyName,  cancellationToken);

        if (companyAlreadyExist)
        {
            ModelState.AddModelError("CompanyName", "The company with the same name already exists.");
            TempData["ErrorMessage"] = "The company with the same name already exists.";
            return View(viewModel);
        }

        var company = new Company
        {
            CompanyName = viewModel.CompanyName,
            CreatedBy = _userName!,
        };
        
        await _dbContext.Companies.AddAsync(company,  cancellationToken);
        
        LogsModel logs = new(_userName!, $"Add new company: {viewModel.CompanyName}");
        await _dbContext.Logs.AddAsync(logs, cancellationToken);
        
        await _dbContext.SaveChangesAsync(cancellationToken);
        TempData["success"] = "Company created successfully";
        return RedirectToAction("Index");
    }
    
    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_userName))
        {
            if (_userRole != "admin")
            {
                TempData["ErrorMessage"] = "You have no access to this action. Please contact the MIS Department if you think this is a mistake.";
                return RedirectToAction("Privacy", "Home");
            }
            
            return RedirectToAction("Login", "Account");
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
            CompanyName = company.CompanyName,
        };

        return View(viewModel);
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(CompanyViewModel viewModel, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_userName))
        {
            if (_userRole == "admin")
            {
                return RedirectToAction("Login", "Account");
            }
            
            TempData["ErrorMessage"] = "You have no access to this action. Please contact the MIS Department if you think this is a mistake.";
            return RedirectToAction("Privacy", "Home");
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
            .AnyAsync(u => u.CompanyName == viewModel.CompanyName,  cancellationToken);

        if (companyAlreadyExist)
        {
            ModelState.AddModelError("CompanyName", "The company with the same name already exists.");
            TempData["ErrorMessage"] = "The company with the same name already exists.";
            return RedirectToAction("Edit");
        }
        
        var existingName = existingCompany.CompanyName;
        existingCompany.CompanyName = viewModel.CompanyName;
        existingCompany.EditedBy = _userName;
        existingCompany.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();
        
        LogsModel logs = new(_userName, $"Update company from {existingName} to {viewModel.CompanyName}");
        await _dbContext.Logs.AddAsync(logs, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
        TempData["success"] = "Company updated successfully";
        return RedirectToAction("Index");

    }
}