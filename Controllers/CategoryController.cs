using Document_Management.Data;
using Document_Management.Models;
using Document_Management.Utility.Helper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Document_Management.Controllers;

public class CategoryController : Controller
{
    private readonly ApplicationDbContext _dbContext;
    private readonly string? _userRole;
    private readonly string? _userName;

    public CategoryController(ApplicationDbContext dbContext, IHttpContextAccessor httpContextAccessor)
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

        var category = await _dbContext.Categories
            .OrderBy(u => u.CategoryName)
            .ToListAsync();

        return View(category);
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
    public async Task<IActionResult> Create(CategoryViewModel viewModel, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "The information you submitted is not valid.";
            return View(viewModel);
        }
        
        var categoryAlreadyExist = await _dbContext.Categories
            .AnyAsync(u => u.CategoryName == viewModel.CategoryName,  cancellationToken);

        if (categoryAlreadyExist)
        {
            ModelState.AddModelError("CategoryName", "The category with the same name already exists.");
            TempData["ErrorMessage"] = "The category with the same name already exists.";
            return View(viewModel);
        }

        var category = new Category
        {
            CategoryName = viewModel.CategoryName,
            CreatedBy = _userName!,
        };
        
        await _dbContext.Categories.AddAsync(category,  cancellationToken);
        
        LogsModel logs = new(_userName!, $"Add new category: {viewModel.CategoryName}");
        await _dbContext.Logs.AddAsync(logs, cancellationToken);
        
        await _dbContext.SaveChangesAsync(cancellationToken);
        TempData["success"] = "Category created successfully";
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
            
        var category = await _dbContext.Categories
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (category == null)
        {
            return NotFound();
        }

        var viewModel = new CategoryViewModel
        {
            Id = category.Id,
            CategoryName = category.CategoryName,
        };

        return View(viewModel);
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(CategoryViewModel viewModel, CancellationToken cancellationToken)
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
        
        var existingCategory = await _dbContext.Categories
            .FirstOrDefaultAsync(x => x.Id == viewModel.Id, cancellationToken);

        if (existingCategory == null)
        {
            return NotFound();
        }
        
        var categoryAlreadyExist = await _dbContext.Categories
            .AnyAsync(u => u.CategoryName == viewModel.CategoryName,  cancellationToken);

        if (categoryAlreadyExist)
        {
            ModelState.AddModelError("CategoryName", "The category with the same name already exists.");
            TempData["ErrorMessage"] = "The category with the same name already exists.";
            return RedirectToAction("Edit");
        }
        
        var existingName = existingCategory.CategoryName;
        existingCategory.CategoryName = viewModel.CategoryName;
        existingCategory.EditedBy = _userName;
        existingCategory.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();
        
        LogsModel logs = new(_userName, $"Update category from {existingName} to {viewModel.CategoryName}");
        await _dbContext.Logs.AddAsync(logs, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
        TempData["success"] = "Category updated successfully";
        return RedirectToAction("Index");

    }
}