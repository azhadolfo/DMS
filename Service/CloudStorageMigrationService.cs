using Document_Management.Data;
using Document_Management.Models;
using Microsoft.EntityFrameworkCore;

namespace Document_Management.Service;

public class CloudStorageMigrationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ICloudStorageService _cloudStorage;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<CloudStorageMigrationService> _logger;

        public CloudStorageMigrationService(
            ApplicationDbContext context,
            ICloudStorageService cloudStorage,
            IWebHostEnvironment environment,
            ILogger<CloudStorageMigrationService> logger)
        {
            _context = context;
            _cloudStorage = cloudStorage;
            _environment = environment;
            _logger = logger;
        }

        public async Task<MigrationResult> MigrateAllFilesToCloudAsync(CancellationToken cancellationToken = default)
        {
            var result = new MigrationResult();
            
            try
            {
                // Get all files that are not yet in cloud storage
                var filesToMigrate = await _context.FileDocuments
                    .Where(f => !f.IsInCloudStorage)
                    .ToListAsync(cancellationToken);

                _logger.LogInformation($"Starting migration of {filesToMigrate.Count} files to Cloud Storage");

                foreach (var fileDocument in filesToMigrate)
                {
                    try
                    {
                        await MigrateFileToCloudAsync(fileDocument, cancellationToken);
                        result.SuccessCount++;
                        
                        // Log progress every 10 files
                        if (result.SuccessCount % 10 == 0)
                        {
                            _logger.LogInformation($"Migrated {result.SuccessCount}/{filesToMigrate.Count} files");
                        }
                    }
                    catch (Exception ex)
                    {
                        result.FailedFiles.Add(new FailedMigration
                        {
                            FileId = fileDocument.Id,
                            FileName = fileDocument.Name ?? "Unknown",
                            Error = ex.Message
                        });
                        result.FailureCount++;
                        _logger.LogError(ex, $"Failed to migrate file {fileDocument.Id}: {fileDocument.Name}");
                    }
                }

                await _context.SaveChangesAsync(cancellationToken);
                
                _logger.LogInformation($"Migration completed. Success: {result.SuccessCount}, Failed: {result.FailureCount}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Migration process failed");
                result.GeneralError = ex.Message;
            }

            return result;
        }

        private async Task MigrateFileToCloudAsync(FileDocument fileDocument, CancellationToken cancellationToken)
        {
            // Check if local file exists
            var localFilePath = fileDocument.Location;
            if (string.IsNullOrEmpty(localFilePath) || !File.Exists(localFilePath))
            {
                throw new FileNotFoundException($"Local file not found: {localFilePath}");
            }

            // Create cloud storage path
            var cloudStoragePath = fileDocument.SubCategory == "N/A"
                ? $"Files/{fileDocument.Company}/{fileDocument.Year}/{fileDocument.Department}/{fileDocument.Category}/{fileDocument.Name}"
                : $"Files/{fileDocument.Company}/{fileDocument.Year}/{fileDocument.Department}/{fileDocument.Category}/{fileDocument.SubCategory}/{fileDocument.Name}";

            // Read local file and create IFormFile
            var fileBytes = await File.ReadAllBytesAsync(localFilePath, cancellationToken);
            using var stream = new MemoryStream(fileBytes);
            
            var formFile = new FormFile(stream, 0, fileBytes.Length, "file", fileDocument.Name ?? "file")
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/pdf"
            };

            // Upload to Cloud Storage
            var objectName = await _cloudStorage.UploadFileAsync(formFile, cloudStoragePath);

            // Update database record
            fileDocument.Location = objectName;
            fileDocument.IsInCloudStorage = true;

            // Optionally delete local file after successful upload
            // Uncomment the next line if you want to delete local files immediately
            // File.Delete(localFilePath);
        }

        public async Task<int> GetPendingMigrationCountAsync(CancellationToken cancellationToken = default)
        {
            return await _context.FileDocuments
                .CountAsync(f => !f.IsInCloudStorage, cancellationToken);
        }
    }

    public class MigrationResult
    {
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<FailedMigration> FailedFiles { get; set; } = new();
        public string? GeneralError { get; set; }
        public bool IsSuccess => FailureCount == 0 && string.IsNullOrEmpty(GeneralError);
    }

    public class FailedMigration
    {
        public int FileId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
    }