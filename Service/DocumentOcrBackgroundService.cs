using Document_Management.Data;

namespace Document_Management.Service
{
    public sealed class DocumentOcrBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<DocumentOcrBackgroundService> _logger;
        private readonly TimeSpan _idleDelay;

        public DocumentOcrBackgroundService(
            IServiceScopeFactory serviceScopeFactory,
            ILogger<DocumentOcrBackgroundService> logger,
            IConfiguration configuration)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
            _idleDelay = TimeSpan.FromSeconds(configuration.GetValue("OcrWorker:PollIntervalSeconds", 10));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceScopeFactory.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    if (!await dbContext.Database.CanConnectAsync(stoppingToken))
                    {
                        await Task.Delay(_idleDelay, stoppingToken);
                        continue;
                    }

                    var documentOcrService = scope.ServiceProvider.GetRequiredService<IDocumentOcrService>();
                    var documentId = await documentOcrService.TryClaimNextDocumentAsync(stoppingToken);

                    if (!documentId.HasValue)
                    {
                        await Task.Delay(_idleDelay, stoppingToken);
                        continue;
                    }

                    await documentOcrService.ProcessDocumentAsync(documentId.Value, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unhandled error in OCR background worker.");
                    await Task.Delay(_idleDelay, stoppingToken);
                }
            }
        }
    }
}
