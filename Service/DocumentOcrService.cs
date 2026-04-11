using Document_Management.Data;
using Document_Management.Models;
using Document_Management.Utility.Helper;
using Microsoft.EntityFrameworkCore;

namespace Document_Management.Service
{
    public interface IDocumentOcrService
    {
        Task<int?> TryClaimNextDocumentAsync(CancellationToken cancellationToken);
        Task ProcessDocumentAsync(int documentId, CancellationToken cancellationToken);
        Task<int> ProcessPendingDocumentsAsync(int maxDocuments, CancellationToken cancellationToken);
    }

    public sealed class DocumentOcrService : IDocumentOcrService
    {
        private static readonly TimeSpan StaleProcessingThreshold = TimeSpan.FromMinutes(30);
        private readonly ApplicationDbContext _dbContext;
        private readonly ICloudStorageService _cloudStorageService;
        private readonly IPdfTextExtractionService _pdfTextExtractionService;
        private readonly ILogger<DocumentOcrService> _logger;

        public DocumentOcrService(
            ApplicationDbContext dbContext,
            ICloudStorageService cloudStorageService,
            IPdfTextExtractionService pdfTextExtractionService,
            ILogger<DocumentOcrService> logger)
        {
            _dbContext = dbContext;
            _cloudStorageService = cloudStorageService;
            _pdfTextExtractionService = pdfTextExtractionService;
            _logger = logger;
        }

        public async Task<int?> TryClaimNextDocumentAsync(CancellationToken cancellationToken)
        {
            var timeNow = DateTimeHelper.GetCurrentPhilippineTime();
            var staleProcessingCutoff = timeNow.Subtract(StaleProcessingThreshold);

            var document = await _dbContext.FileDocuments
                .Where(file =>
                    !file.IsDeleted &&
                    file.IsInCloudStorage &&
                    (file.OcrStatus == OcrStatuses.Pending ||
                     (file.OcrStatus == OcrStatuses.Processing && file.OcrStartedAt != null && file.OcrStartedAt < staleProcessingCutoff)))
                .OrderBy(file => file.OcrQueuedAt ?? file.DateUploaded)
                .FirstOrDefaultAsync(cancellationToken);

            if (document == null)
            {
                return null;
            }

            document.OcrStatus = OcrStatuses.Processing;
            document.OcrStartedAt = timeNow;
            document.OcrCompletedAt = null;
            document.OcrAttemptCount += 1;
            document.OcrError = string.Empty;

            await _dbContext.SaveChangesAsync(cancellationToken);
            return document.Id;
        }

        public async Task ProcessDocumentAsync(int documentId, CancellationToken cancellationToken)
        {
            var document = await _dbContext.FileDocuments
                .FirstOrDefaultAsync(file => file.Id == documentId, cancellationToken);

            if (document == null)
            {
                return;
            }

            try
            {
                await using var downloadStream = await _cloudStorageService.DownloadFileStreamAsync(document.Location, cancellationToken);
                using var memoryStream = new MemoryStream();
                await downloadStream.CopyToAsync(memoryStream, cancellationToken);
                var fileBytes = memoryStream.ToArray();

                document.ExtractedText = await _pdfTextExtractionService.ExtractTextAsync(fileBytes, "application/pdf", cancellationToken);
                document.OcrStatus = OcrStatuses.Completed;
                document.OcrCompletedAt = DateTimeHelper.GetCurrentPhilippineTime();
                document.OcrError = string.Empty;

                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                document.OcrStatus = OcrStatuses.Failed;
                document.OcrCompletedAt = DateTimeHelper.GetCurrentPhilippineTime();
                document.OcrError = TruncateError(ex.Message);

                await _dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogError(ex, "OCR processing failed for document {DocumentId}", documentId);
            }
        }

        public async Task<int> ProcessPendingDocumentsAsync(int maxDocuments, CancellationToken cancellationToken)
        {
            var processedCount = 0;

            while (processedCount < maxDocuments)
            {
                var documentId = await TryClaimNextDocumentAsync(cancellationToken);
                if (!documentId.HasValue)
                {
                    break;
                }

                await ProcessDocumentAsync(documentId.Value, cancellationToken);
                processedCount += 1;
            }

            return processedCount;
        }

        private static string TruncateError(string error)
        {
            const int maxLength = 4000;
            return error.Length <= maxLength ? error : error[..maxLength];
        }
    }
}
