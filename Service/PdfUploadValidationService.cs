using Microsoft.AspNetCore.Http;
using UglyToad.PdfPig;

namespace Document_Management.Service
{
    public interface IPdfUploadValidationService
    {
        Task<PdfUploadValidationResult> ValidateAsync(IFormFile file, CancellationToken cancellationToken);
    }

    public sealed class PdfUploadValidationService : IPdfUploadValidationService
    {
        private const long _maxFileSizeBytes = 20 * 1024 * 1024;
        private static readonly byte[] _pdfHeader = "%PDF-"u8.ToArray();
        private static readonly HashSet<string> _allowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "application/pdf",
            "application/x-pdf",
            "application/octet-stream"
        };

        private readonly ILogger<PdfUploadValidationService> _logger;

        public PdfUploadValidationService(ILogger<PdfUploadValidationService> logger)
        {
            _logger = logger;
        }

        public async Task<PdfUploadValidationResult> ValidateAsync(IFormFile file, CancellationToken cancellationToken)
        {
            if (file.Length <= 0)
            {
                return PdfUploadValidationResult.Invalid("Please select a PDF file.");
            }

            if (file.Length > _maxFileSizeBytes)
            {
                return PdfUploadValidationResult.Invalid("File is too large 20MB is the maximum size allowed.");
            }

            var extension = Path.GetExtension(file.FileName);
            if (!string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase))
            {
                return PdfUploadValidationResult.Invalid("Please upload pdf file only!");
            }

            if (!string.IsNullOrWhiteSpace(file.ContentType) && !_allowedContentTypes.Contains(file.ContentType))
            {
                return PdfUploadValidationResult.Invalid("Please upload a valid PDF file.");
            }

            await using var inputStream = file.OpenReadStream();
            using var memoryStream = new MemoryStream();
            await inputStream.CopyToAsync(memoryStream, cancellationToken);
            var fileBytes = memoryStream.ToArray();

            if (!HasPdfHeader(fileBytes))
            {
                return PdfUploadValidationResult.Invalid("The uploaded file is not a valid PDF.");
            }

            try
            {
                using var pdfStream = new MemoryStream(fileBytes, writable: false);
                using var document = PdfDocument.Open(pdfStream);
                var pageCount = document.NumberOfPages;

                return pageCount <= 0
                    ? PdfUploadValidationResult.Invalid("The uploaded PDF does not contain any pages.")
                    : PdfUploadValidationResult.Valid(pageCount);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Rejected invalid PDF upload for file {FileName}", file.FileName);
                return PdfUploadValidationResult.Invalid("The uploaded file could not be parsed as a valid PDF.");
            }
        }

        private static bool HasPdfHeader(byte[] fileBytes)
        {
            if (fileBytes.Length < _pdfHeader.Length)
            {
                return false;
            }

            for (var index = 0; index < _pdfHeader.Length; index++)
            {
                if (fileBytes[index] != _pdfHeader[index])
                {
                    return false;
                }
            }

            return true;
        }
    }

    public sealed record PdfUploadValidationResult(bool IsValid, string ErrorMessage, int PageCount)
    {
        public static PdfUploadValidationResult Valid(int pageCount) => new(true, string.Empty, pageCount);

        public static PdfUploadValidationResult Invalid(string errorMessage) => new(false, errorMessage, 0);
    }
}
