using Document_Management.Data;
using Document_Management.Models;
using Microsoft.EntityFrameworkCore;

namespace Document_Management.Service
{
    public interface IDmsSearchService
    {
        Task<GeneralSearchViewModel> SearchAsync(
            string search,
            int page,
            int pageSize,
            string sortBy,
            string sortOrder,
            CancellationToken cancellationToken);
    }

    public sealed class DmsSearchService : IDmsSearchService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IDmsAccessService _accessService;

        public DmsSearchService(ApplicationDbContext dbContext, IDmsAccessService accessService)
        {
            _dbContext = dbContext;
            _accessService = accessService;
        }

        public async Task<GeneralSearchViewModel> SearchAsync(
            string search,
            int page,
            int pageSize,
            string sortBy,
            string sortOrder,
            CancellationToken cancellationToken)
        {
            var keywords = search
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            var query = _dbContext.FileDocuments
                .AsNoTracking()
                .Where(file => !file.IsDeleted);

            foreach (var keyword in keywords)
            {
                var currentKeyword = keyword;
                query = query.Where(file =>
                    file.Description.Contains(currentKeyword) ||
                    file.OriginalFilename.Contains(currentKeyword) ||
                    file.BoxNumber.Contains(currentKeyword));
            }

            var results = await query.ToListAsync(cancellationToken);

            if (!_accessService.IsAdmin())
            {
                results = results
                    .Where(file => _accessService.CanAccessCompany(file.Company) && _accessService.CanAccessDepartment(file.Department))
                    .ToList();
            }

            results = ApplySorting(results, sortBy, sortOrder);

            var totalRecords = results.Count;
            var totalPages = totalRecords == 0
                ? 0
                : (int)Math.Ceiling(totalRecords / (double)pageSize);

            var pagedResults = results
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new GeneralSearchViewModel
            {
                Results = pagedResults,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalRecords = totalRecords,
                PageSize = pageSize,
                SearchTerm = search,
                SortBy = sortBy,
                SortOrder = sortOrder
            };
        }

        private static List<FileDocument> ApplySorting(List<FileDocument> results, string sortBy, string sortOrder)
        {
            return sortBy switch
            {
                "BoxNumber" => sortOrder == "asc"
                    ? results.OrderBy(file => file.BoxNumber).ToList()
                    : results.OrderByDescending(file => file.BoxNumber).ToList(),
                "OriginalFilename" => sortOrder == "asc"
                    ? results.OrderBy(file => file.OriginalFilename).ToList()
                    : results.OrderByDescending(file => file.OriginalFilename).ToList(),
                "Description" => sortOrder == "asc"
                    ? results.OrderBy(file => file.Description).ToList()
                    : results.OrderByDescending(file => file.Description).ToList(),
                "Username" => sortOrder == "asc"
                    ? results.OrderBy(file => file.Username).ToList()
                    : results.OrderByDescending(file => file.Username).ToList(),
                "DateUploaded" => sortOrder == "asc"
                    ? results.OrderBy(file => file.DateUploaded).ToList()
                    : results.OrderByDescending(file => file.DateUploaded).ToList(),
                _ => results.OrderByDescending(file => file.DateUploaded).ToList()
            };
        }
    }
}
