using Google.Cloud.Storage.V1;
using Google.Apis.Auth.OAuth2;
using System.Net;
using Google;
using Object = Google.Apis.Storage.v1.Data.Object;

namespace Document_Management.Services
{
    public interface ICloudStorageService
    {
        Task<string> UploadFileAsync(IFormFile file, string objectName);
        Task<Stream> DownloadFileStreamAsync(string objectName);
        Task<bool> DeleteFileAsync(string objectName);
        Task<string> GetSignedUrlAsync(string objectName, TimeSpan expiry);
    }

    public class GoogleCloudStorageService : ICloudStorageService
    {
        private readonly StorageClient _storageClient;
        private readonly string _bucketName;
        private readonly ILogger<GoogleCloudStorageService> _logger;

        public GoogleCloudStorageService(ILogger<GoogleCloudStorageService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _bucketName = configuration["GoogleCloudStorage:BucketName"] ??
                          throw new ArgumentException("GoogleCloudStorage:BucketName configuration is required");

            // Use default credentials in Cloud Run (no service account file needed)
            _storageClient = StorageClient.Create();
        }

        public async Task<string> UploadFileAsync(IFormFile file, string objectName)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty or null");

            try
            {
                using var stream = file.OpenReadStream();

                var googleObject = new Object
                {
                    Bucket = _bucketName,
                    Name = objectName,
                    ContentType = file.ContentType
                };

                await _storageClient.UploadObjectAsync(googleObject, stream);

                _logger.LogInformation($"File uploaded successfully to Cloud Storage: {objectName}");
                return objectName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error uploading file to Cloud Storage: {objectName}");
                throw;
            }
        }

        public async Task<Stream> DownloadFileStreamAsync(string objectName)
        {
            try
            {
                var memoryStream = new MemoryStream();
                await _storageClient.DownloadObjectAsync(_bucketName, objectName, memoryStream);
                memoryStream.Position = 0;
                return memoryStream;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error downloading file from Cloud Storage: {objectName}");
                throw;
            }
        }

        public async Task<bool> DeleteFileAsync(string objectName)
        {
            try
            {
                await _storageClient.DeleteObjectAsync(_bucketName, objectName);
                _logger.LogInformation($"File deleted successfully from Cloud Storage: {objectName}");
                return true;
            }
            catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning($"File not found in Cloud Storage for deletion: {objectName}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting file from Cloud Storage: {objectName}");
                throw;
            }
        }

        public async Task<string> GetSignedUrlAsync(string objectName, TimeSpan expiry)
        {
            try
            {
                var credential = GoogleCredential.GetApplicationDefault();
                var urlSigner = UrlSigner.FromCredential(credential);

                var signedUrl = await urlSigner.SignAsync(_bucketName, objectName, expiry, HttpMethod.Get);

                _logger.LogInformation($"Generated signed URL for: {objectName}");
                return signedUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating signed URL for: {objectName}");
                throw;
            }
        }
    }
}