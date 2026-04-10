using System.Globalization;
using Document_Management.Data;
using Document_Management.Dtos;
using Document_Management.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;

namespace Document_Management.Service
{
    public interface IDmsQueryService
    {
        Task<List<string>> GetAccessibleCompaniesAsync(CancellationToken cancellationToken);
        Task<List<string>> GetYearsAsync(string company, CancellationToken cancellationToken);
        Task<List<string>> GetAccessibleDepartmentsAsync(string company, string year, CancellationToken cancellationToken);
        Task<List<CategoryDto>> GetCategoriesAsync(string company, string year, string department, CancellationToken cancellationToken);
        Task<List<string>> GetSubCategoriesAsync(string company, string year, string department, string category, CancellationToken cancellationToken);
        Task<List<FileDocument>> GetFilesAsync(string company, string year, string department, string category, string? subCategory, string? fileName, CancellationToken cancellationToken);
        Task<DataTableResult<UploadedFilesViewModel>> GetUploadedFilesAsync(DataTablesParameters parameters, CancellationToken cancellationToken);
        Task<DataTableResult<UploadedFilesViewModel>> GetDeletedFilesAsync(DataTablesParameters parameters, CancellationToken cancellationToken);
    }

    public sealed class DmsQueryService : IDmsQueryService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IDmsAccessService _accessService;

        public DmsQueryService(ApplicationDbContext dbContext, IDmsAccessService accessService)
        {
            _dbContext = dbContext;
            _accessService = accessService;
        }

        public async Task<List<string>> GetAccessibleCompaniesAsync(CancellationToken cancellationToken)
        {
            var companies = await _dbContext.FileDocuments
                .AsNoTracking()
                .Where(f => !string.IsNullOrEmpty(f.Company))
                .Select(f => f.Company)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync(cancellationToken);

            return companies
                .Where(company => _accessService.CanAccessCompany(company))
                .ToList();
        }

        public Task<List<string>> GetYearsAsync(string company, CancellationToken cancellationToken)
        {
            return _dbContext.FileDocuments
                .AsNoTracking()
                .Where(f => f.Company == company && !string.IsNullOrEmpty(f.Year))
                .Select(f => f.Year)
                .Distinct()
                .OrderByDescending(y => y)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<string>> GetAccessibleDepartmentsAsync(string company, string year, CancellationToken cancellationToken)
        {
            var departments = await _dbContext.FileDocuments
                .AsNoTracking()
                .Where(f => f.Company == company &&
                            f.Year == year &&
                            !string.IsNullOrEmpty(f.Department))
                .Select(f => f.Department)
                .Distinct()
                .OrderBy(d => d)
                .ToListAsync(cancellationToken);

            return departments
                .Where(department => _accessService.CanAccessDepartment(department))
                .ToList();
        }

        public Task<List<CategoryDto>> GetCategoriesAsync(string company, string year, string department, CancellationToken cancellationToken)
        {
            return _dbContext.FileDocuments
                .AsNoTracking()
                .Where(f => f.Company == company &&
                            f.Year == year &&
                            f.Department == department &&
                            !string.IsNullOrEmpty(f.Category))
                .Select(f => new CategoryDto
                {
                    Category = f.Category,
                    SubCategory = f.SubCategory
                })
                .Distinct()
                .OrderBy(c => c.Category)
                .ToListAsync(cancellationToken);
        }

        public Task<List<string>> GetSubCategoriesAsync(string company, string year, string department, string category, CancellationToken cancellationToken)
        {
            return _dbContext.FileDocuments
                .AsNoTracking()
                .Where(f => f.Company == company &&
                            f.Year == year &&
                            f.Department == department &&
                            f.Category == category &&
                            !string.IsNullOrEmpty(f.SubCategory) &&
                            f.SubCategory != "N/A")
                .Select(f => f.SubCategory)
                .Distinct()
                .OrderBy(s => s)
                .ToListAsync(cancellationToken);
        }

        public Task<List<FileDocument>> GetFilesAsync(string company, string year, string department, string category, string? subCategory, string? fileName, CancellationToken cancellationToken)
        {
            var query = _dbContext.FileDocuments
                .AsNoTracking()
                .Where(file => file.Company == company
                               && file.Year == year
                               && file.Department == department
                               && file.Category == category
                               && !file.IsDeleted);

            if (!string.IsNullOrEmpty(subCategory))
            {
                query = query.Where(file => file.SubCategory == subCategory);
            }
            else
            {
                query = query.Where(file => file.SubCategory == "N/A" || string.IsNullOrEmpty(file.SubCategory));
            }

            if (!string.IsNullOrEmpty(fileName))
            {
                query = query.Where(file => file.Name == fileName);
            }

            return query
                .Select(file => new FileDocument
                {
                    Id = file.Id,
                    Name = file.Name,
                    Location = file.Location,
                    DateUploaded = file.DateUploaded,
                    Description = file.Description,
                    Department = file.Department,
                    Username = file.Username,
                    Category = file.Category,
                    Company = file.Company,
                    Year = file.Year,
                    SubCategory = file.SubCategory,
                    OriginalFilename = file.OriginalFilename,
                    FileSize = file.FileSize,
                    NumberOfPages = file.NumberOfPages
                })
                .OrderByDescending(file => file.DateUploaded)
                .ToListAsync(cancellationToken);
        }

        public Task<DataTableResult<UploadedFilesViewModel>> GetUploadedFilesAsync(DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            return GetFileTableAsync(parameters, includeDeleted: false, cancellationToken);
        }

        public Task<DataTableResult<UploadedFilesViewModel>> GetDeletedFilesAsync(DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            return GetFileTableAsync(parameters, includeDeleted: true, cancellationToken);
        }

        private async Task<DataTableResult<UploadedFilesViewModel>> GetFileTableAsync(DataTablesParameters parameters, bool includeDeleted, CancellationToken cancellationToken)
        {
            var username = _accessService.Username;
            var filesQuery = _dbContext.FileDocuments
                .AsNoTracking()
                .Where(file => file.IsDeleted == includeDeleted);

            if (!_accessService.IsAdmin())
            {
                filesQuery = filesQuery.Where(file => file.Username == username);
            }

            var files = await filesQuery.ToListAsync(cancellationToken);
            var viewModel = files
                .Select(MapUploadedFile)
                .ToList();

            var totalRecordsBeforeSearch = files.Count;
            if (!string.IsNullOrWhiteSpace(parameters.Search.Value))
            {
                var searchValue = parameters.Search.Value.ToLowerInvariant();
                viewModel = viewModel.Where(f =>
                        f.Name.ToLowerInvariant().Contains(searchValue) ||
                        f.Description.ToLowerInvariant().Contains(searchValue) ||
                        f.DateUploaded.ToString(CultureInfo.InvariantCulture).Contains(searchValue))
                    .ToList();
            }

            if (parameters.Order.Count > 0)
            {
                var orderColumn = parameters.Order[0];

                if (orderColumn.Column >= 0 && orderColumn.Column < parameters.Columns.Count)
                {
                    var columnName = parameters.Columns[orderColumn.Column].Data;
                    var sortDirection = orderColumn.Dir.Equals("asc", StringComparison.CurrentCultureIgnoreCase)
                        ? "ascending"
                        : "descending";

                    viewModel = viewModel.AsQueryable().OrderBy($"{columnName} {sortDirection}").ToList();
                }
            }

            var recordsFiltered = viewModel.Count;
            var pagedData = viewModel
                .Skip(parameters.Start)
                .Take(parameters.Length)
                .ToList();

            return new DataTableResult<UploadedFilesViewModel>(parameters.Draw, totalRecordsBeforeSearch, recordsFiltered, pagedData);
        }

        private static UploadedFilesViewModel MapUploadedFile(FileDocument file)
        {
            return new UploadedFilesViewModel
            {
                Id = file.Id,
                Name = file.Name,
                Description = file.Description,
                LocationFolder = file.SubCategory == "N/A"
                    ? $"companyFolderName={file.Company}&yearFolderName={file.Year}&departmentFolderName={file.Department}&documentTypeFolderName={file.Category}&subCategoryFolder={null}&fileName={file.Name}"
                    : $"companyFolderName={file.Company}&yearFolderName={file.Year}&departmentFolderName={file.Department}&documentTypeFolderName={file.Category}&subCategoryFolder={file.SubCategory}&fileName={file.Name}",
                UploadedBy = file.Username,
                DateUploaded = file.DateUploaded,
                BoxNumber = file.BoxNumber,
                SubmittedBy = file.SubmittedBy,
                DateSubmitted = file.DateSubmitted ?? default
            };
        }
    }

    public sealed record DataTableResult<T>(int Draw, int RecordsTotal, int RecordsFiltered, IReadOnlyCollection<T> Data);
}
