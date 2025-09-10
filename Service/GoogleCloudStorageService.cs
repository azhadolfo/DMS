using Google.Cloud.Storage.V1;
using Google.Apis.Auth.OAuth2;
using System.Net;
using Google;
using Object = Google.Apis.Storage.v1.Data.Object;

namespace Document_Management.Service
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
        private readonly GoogleCredential _googleCredential;

        public GoogleCloudStorageService(ILogger<GoogleCloudStorageService> logger,
            IConfiguration configuration)
        {
            _logger = logger;
            _bucketName = configuration["GoogleCloudStorage:BucketName"] ??
                          throw new ArgumentException("GoogleCloudStorage:BucketName configuration is required");

            try
            {
                var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
                if (environment == Environments.Production)
                {
                    _googleCredential = GoogleCredential.GetApplicationDefault();
                    _logger.LogInformation("Using default application credentials (Cloud Run).");
                }
                else
                {
                    var credentialPath = configuration["GoogleCloudStorage:CredentialPath"];
                    if (string.IsNullOrEmpty(credentialPath) || !File.Exists(credentialPath))
                    {
                        throw new FileNotFoundException("Service account credential file not found", credentialPath);
                    }
                
                    _googleCredential = GoogleCredential.FromFile(credentialPath);
                    _logger.LogInformation("Using service account credentials from file (local dev).");
                }

                _storageClient = StorageClient.Create(_googleCredential);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Google Cloud Storage client");
                throw;
            }
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

                _logger.LogInformation("File uploaded successfully to Cloud Storage: {ObjectName}", objectName);
                return objectName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file to Cloud Storage: {ObjectName}", objectName);
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
                _logger.LogError(ex, "Error downloading file from Cloud Storage: {ObjectName}", objectName);
                throw;
            }
        }

        public async Task<bool> DeleteFileAsync(string objectName)
        {
            try
            {
                await _storageClient.DeleteObjectAsync(_bucketName, objectName);
                _logger.LogInformation("File deleted successfully from Cloud Storage: {ObjectName}", objectName);
                return true;
            }
            catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("File not found in Cloud Storage for deletion: {ObjectName}", objectName);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file from Cloud Storage: {ObjectName}", objectName);
                throw;
            }
        }

        public async Task<string> GetSignedUrlAsync(string objectName, TimeSpan expiry)
        {
            try
            {
                var urlSigner = UrlSigner.FromCredential(_googleCredential);

                var signedUrl = await urlSigner.SignAsync(_bucketName, objectName, expiry, HttpMethod.Get);

                _logger.LogInformation("Generated signed URL for: {ObjectName}", objectName);
                return signedUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating signed URL for: {ObjectName}", objectName);
                throw;
            }
        }
    }
}
