using Microsoft.AspNetCore.Mvc.Rendering;

namespace Document_Management.Models;

public class SubCategoryViewModel
{
    public int Id { get; set; }

    public string SubCategoryName { get; set; } = string.Empty;

    public List<SelectListItem>? Categories { get; set; }

    public int CategoryId { get; set; }
}