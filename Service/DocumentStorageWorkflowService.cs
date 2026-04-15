using Document_Management.Models;
using Document_Management.Utility.Helper;
using Microsoft.AspNetCore.Http;

namespace Document_Management.Service
{
    public interface IDocumentStorageWorkflowService
    {
        Task<UploadDocumentResult> UploadAsync(FileDocument document, IFormFile file, CancellationToken cancellationToken);
        Task<ReplaceDocumentResult> ReplaceFileAsync(FileDocument existingDocument, IFormFile newFile, CancellationToken cancellationToken);
        Task<TransferDocumentResult> TransferAsync(FileDocument existingDocument, FileDocument destination, CancellationToken cancellationToken);
    }

    public sealed class DocumentStorageWorkflowService : IDocumentStorageWorkflowService
    {
        private readonly ICloudStorageService _cloudStorageService;
        private readonly ILogger<DocumentStorageWorkflowService> _logger;

        public DocumentStorageWorkflowService(
            ICloudStorageService cloudStorageService,
            ILogger<DocumentStorageWorkflowService> logger)
        {
            _cloudStorageService = cloudStorageService;
            _logger = logger;
        }

        public async Task<UploadDocumentResult> UploadAsync(FileDocument document, IFormFile file, CancellationToken cancellationToken)
        {
            var uploadedAt = DateTimeHelper.GetCurrentPhilippineTime();
            var fileName = BuildStoredFileName(document.Department, file.FileName, uploadedAt);
            var storagePath = BuildStoragePath(document.Company, document.Year, document.Department, document.Category, document.SubCategory, fileName);
            var objectName = await _cloudStorageService.UploadFileAsync(file, storagePath, cancellationToken);

            return new UploadDocumentResult(
                fileName,
                objectName,
                file.Length,
                uploadedAt,
                BuildFolderPath(document.Company, document.Year, document.Department, document.Category, document.SubCategory));
        }

        public async Task<ReplaceDocumentResult> ReplaceFileAsync(FileDocument existingDocument, IFormFile newFile, CancellationToken cancellationToken)
        {
            await TryDeleteExistingObjectAsync(existingDocument.Location);

            var fileName = BuildStoredFileName(existingDocument.Department, newFile.FileName, DateTimeHelper.GetCurrentPhilippineTime());
            var storagePath = BuildStoragePath(existingDocument.Company, existingDocument.Year, existingDocument.Department, existingDocument.Category, existingDocument.SubCategory, fileName);
            var objectName = await _cloudStorageService.UploadFileAsync(newFile, storagePath, cancellationToken);

            return new ReplaceDocumentResult(fileName, objectName, newFile.Length, newFile.FileName);
        }

        public async Task<TransferDocumentResult> TransferAsync(FileDocument existingDocument, FileDocument destination, CancellationToken cancellationToken)
        {
            var oldLocation = existingDocument.Location;
            await using var fileStream = await _cloudStorageService.DownloadFileStreamAsync(oldLocation, cancellationToken);

            var fileName = BuildStoredFileName(destination.Department, existingDocument.OriginalFilename, existingDocument.DateUploaded);
            var storagePath = BuildStoragePath(destination.Company, destination.Year, destination.Department, destination.Category, destination.SubCategory, fileName);

            using var memoryStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryStream, cancellationToken);
            var fileBytes = memoryStream.ToArray();

            using var uploadStream = new MemoryStream(fileBytes);
            var formFile = new FormFile(uploadStream, 0, fileBytes.Length, "file", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/pdf"
            };

            var newObjectName = await _cloudStorageService.UploadFileAsync(formFile, storagePath, cancellationToken);
            await _cloudStorageService.DeleteFileAsync(oldLocation, cancellationToken);

            return new TransferDocumentResult(fileName, oldLocation, newObjectName);
        }

        private async Task TryDeleteExistingObjectAsync(string? objectName)
        {
            if (string.IsNullOrWhiteSpace(objectName))
            {
                return;
            }

            try
            {
                await _cloudStorageService.DeleteFileAsync(objectName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not delete old file from cloud storage: {ObjectName}", objectName);
            }
        }

        private static string BuildStoredFileName(string department, string originalFileName, DateTime timestamp)
        {
            var sanitizedOriginalFileName = Path.GetFileName(originalFileName).Replace("#", string.Empty);
            return $"{department}_{timestamp:yyyyMMddHHmmssfff}_{sanitizedOriginalFileName}";
        }

        private static string BuildStoragePath(string company, string year, string department, string category, string subCategory, string fileName)
        {
            var parts = new List<string>
            {
                "Files",
                SanitizePathPart(company),
                SanitizePathPart(year),
                SanitizePathPart(department),
                SanitizePathPart(category)
            };

            if (!string.Equals(subCategory, "N/A", StringComparison.OrdinalIgnoreCase))
            {
                parts.Add(SanitizePathPart(subCategory));
            }

            parts.Add(fileName);
            return string.Join("/", parts);
        }

        private static string BuildFolderPath(string company, string year, string department, string category, string subCategory)
        {
            var parts = new List<string>
            {
                SanitizePathPart(company),
                SanitizePathPart(year),
                SanitizePathPart(department),
                SanitizePathPart(category)
            };

            if (!string.Equals(subCategory, "N/A", StringComparison.OrdinalIgnoreCase))
            {
                parts.Add(SanitizePathPart(subCategory));
            }

            return string.Join("/", parts);
        }

        private static string SanitizePathPart(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return "N_A";
            }

            var invalidChars = new[] { "/", "\\", " ", "#", "?", "%", "&", "+", ":", ";", "=", "|", "\"", "<", ">", "*" };
            foreach (var ch in invalidChars)
            {
                input = input.Replace(ch, "_");
            }

            return input.Trim('_');
        }
    }

    public sealed record UploadDocumentResult(
        string StoredFileName,
        string ObjectName,
        long FileSize,
        DateTime UploadedAt,
        string FolderPath);

    public sealed record ReplaceDocumentResult(
        string StoredFileName,
        string ObjectName,
        long FileSize,
        string OriginalFileName);

    public sealed record TransferDocumentResult(
        string StoredFileName,
        string OldLocation,
        string NewLocation);
}
