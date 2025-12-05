using Document_Management.Data;
using Document_Management.Models;
using Document_Management.Utility.Helper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Document_Management.Controllers;

public class SubCategoryController : Controller
{
    private readonly ApplicationDbContext _dbContext;
    private readonly string? _userRole;
    private readonly string? _userName;

    public SubCategoryController(ApplicationDbContext dbContext, IHttpContextAccessor httpContextAccessor)
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

        var subCategory = await _dbContext.SubCategories
            .Include(s => s.Category)
            .OrderBy(u => u.SubCategoryName)
            .ToListAsync();

        return View(subCategory);
    }
    
    [HttpGet]
    public async Task<IActionResult> Create()
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
        
        var viewModel = new SubCategoryViewModel
        {
            Categories = await _dbContext.Categories
                .OrderBy(u => u.CategoryName)
                .Select(c => new SelectListItem
                {
                    Text = c.CategoryName,
                    Value = c.Id.ToString()
                })
                .ToListAsync(),
        };
            
        return View(viewModel);
        
    }

    [HttpPost]
    public async Task<IActionResult> Create(SubCategoryViewModel viewModel, CancellationToken cancellationToken)
    {
        viewModel.Categories = await _dbContext.Categories
            .OrderBy(u => u.CategoryName)
            .Select(c => new SelectListItem
            {
                Text = c.CategoryName,
                Value = c.Id.ToString()
            })
            .ToListAsync(cancellationToken);
        
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "The information you submitted is not valid.";
            return View(viewModel);
        }
        
        var subCategoryAlreadyExist = await _dbContext.SubCategories
            .AnyAsync(u => u.CategoryId == viewModel.CategoryId 
                           && u.SubCategoryName == viewModel.SubCategoryName, cancellationToken);

        if (subCategoryAlreadyExist)
        {
            ModelState.AddModelError("SubCategoryName", "The sub-category with the same name already exists.");
            TempData["ErrorMessage"] = "The sub-category with the same name already exists.";
            return View(viewModel);
        }

        var subCategory = new SubCategory
        {
            SubCategoryName = viewModel.SubCategoryName,
            CategoryId = viewModel.CategoryId,
            CreatedBy = _userName!,
        };
        
        await _dbContext.SubCategories.AddAsync(subCategory,  cancellationToken);
        
        LogsModel logs = new(_userName!, $"Add new sub-category: {viewModel.SubCategoryName}");
        await _dbContext.Logs.AddAsync(logs, cancellationToken);
        
        await _dbContext.SaveChangesAsync(cancellationToken);
        TempData["success"] = "Sub-Category created successfully";
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
            
        var subCategory = await _dbContext.SubCategories
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (subCategory == null)
        {
            return NotFound();
        }

        var viewModel = new SubCategoryViewModel
        {
            Id = subCategory.Id,
            SubCategoryName = subCategory.SubCategoryName,
            CategoryId = subCategory.CategoryId,
            Categories = await _dbContext.Categories
                .OrderBy(u => u.CategoryName)
                .Select(c => new SelectListItem
                {
                    Text = c.CategoryName,
                    Value = c.Id.ToString()
                })
                .ToListAsync(cancellationToken),
        };

        return View(viewModel);
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(SubCategoryViewModel viewModel, CancellationToken cancellationToken)
    {
        viewModel.Categories = await _dbContext.Categories
            .OrderBy(u => u.CategoryName)
            .Select(c => new SelectListItem
            {
                Text = c.CategoryName,
                Value = c.Id.ToString()
            })
            .ToListAsync(cancellationToken);
        
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
        
        var existingSubCategory = await _dbContext.SubCategories
            .FirstOrDefaultAsync(x => x.Id == viewModel.Id, cancellationToken);

        if (existingSubCategory == null)
        {
            return NotFound();
        }
        
        var subCategoryAlreadyExist = await _dbContext.SubCategories
            .AnyAsync(u => u.CategoryId == viewModel.CategoryId 
                           && u.SubCategoryName == viewModel.SubCategoryName, cancellationToken);

        if (subCategoryAlreadyExist)
        {
            ModelState.AddModelError("SubCategoryName", "The sub-category with the same name already exists.");
            TempData["ErrorMessage"] = "The sub-category with the same name already exists.";
            return RedirectToAction("Edit");
        }
        
        var existingName = existingSubCategory.SubCategoryName;
        existingSubCategory.SubCategoryName = viewModel.SubCategoryName;
        existingSubCategory.EditedBy = _userName;
        existingSubCategory.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();
        
        LogsModel logs = new(_userName, $"Update sub-category from {existingName} to {viewModel.SubCategoryName}");
        await _dbContext.Logs.AddAsync(logs, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
        TempData["success"] = "Sub-Category updated successfully";
        return RedirectToAction("Index");

    }
}