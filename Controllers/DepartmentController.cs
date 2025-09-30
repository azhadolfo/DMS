using Document_Management.Data;
using Document_Management.Models;
using Document_Management.Utility.Helper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Document_Management.Controllers;

public class DepartmentController : Controller
{
    private readonly ApplicationDbContext _dbContext;
    private readonly string? _userRole;
    private readonly string? _userName;

    public DepartmentController(ApplicationDbContext dbContext, IHttpContextAccessor httpContextAccessor)
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

        var departments = await _dbContext.Departments
            .OrderBy(u => u.DepartmentName)
            .ToListAsync();

        return View(departments);
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
    public async Task<IActionResult> Create(DepartmentViewModel viewModel, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "The information you submitted is not valid.";
            return View(viewModel);
        }
        
        var departmentAlreadyExist = await _dbContext.Departments
            .AnyAsync(u => u.DepartmentName == viewModel.DepartmentName,  cancellationToken);

        if (departmentAlreadyExist)
        {
            ModelState.AddModelError("DepartmentName", "The department with the same name already exists.");
            TempData["ErrorMessage"] = "The department with the same name already exists.";
            return View(viewModel);
        }

        var department = new Department
        {
            DepartmentName = viewModel.DepartmentName,
            CreatedBy = _userName,
        };
        
        await _dbContext.Departments.AddAsync(department,  cancellationToken);
        
        LogsModel logs = new(_userName, $"Add new department: {viewModel.DepartmentName}");
        await _dbContext.Logs.AddAsync(logs, cancellationToken);
        
        await _dbContext.SaveChangesAsync(cancellationToken);
        TempData["success"] = "Department created successfully";
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
            
        var department = await _dbContext.Departments
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (department == null)
        {
            return NotFound();
        }

        var viewModel = new DepartmentViewModel
        {
            Id = department.Id,
            DepartmentName = department.DepartmentName,
        };

        return View(viewModel);
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(DepartmentViewModel viewModel, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_userName))
        {
            if (_userRole != "admin")
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
        
        var existingDepartment = await _dbContext.Departments
            .FirstOrDefaultAsync(x => x.Id == viewModel.Id, cancellationToken);

        if (existingDepartment == null)
        {
            return NotFound();
        }
        
        var departmentAlreadyExist = await _dbContext.Departments
            .AnyAsync(u => u.DepartmentName == viewModel.DepartmentName,  cancellationToken);

        if (departmentAlreadyExist)
        {
            ModelState.AddModelError("DepartmentName", "The department with the same name already exists.");
            TempData["ErrorMessage"] = "The department with the same name already exists.";
            return RedirectToAction("Edit");
        }
        
        var existingName = existingDepartment.DepartmentName;
        existingDepartment.DepartmentName = viewModel.DepartmentName;
        existingDepartment.EditedBy = _userName;
        existingDepartment.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();
        
        LogsModel logs = new(_userName, $"Update department from {existingName} to {viewModel.DepartmentName}");
        await _dbContext.Logs.AddAsync(logs, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
        TempData["success"] = "Department updated successfully";
        return RedirectToAction("Index");

    }
}