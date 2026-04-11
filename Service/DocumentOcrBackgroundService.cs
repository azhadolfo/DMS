using Document_Management.Data;

namespace Document_Management.Service
{
    public sealed class DocumentOcrBackgroundService : BackgroundService
    {
        private static readonly TimeSpan IdleDelay = TimeSpan.FromSeconds(10);
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<DocumentOcrBackgroundService> _logger;

        public DocumentOcrBackgroundService(
            IServiceScopeFactory serviceScopeFactory,
            ILogger<DocumentOcrBackgroundService> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
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
                        await Task.Delay(IdleDelay, stoppingToken);
                        continue;
                    }

                    var documentOcrService = scope.ServiceProvider.GetRequiredService<IDocumentOcrService>();
                    var documentId = await documentOcrService.TryClaimNextDocumentAsync(stoppingToken);

                    if (!documentId.HasValue)
                    {
                        await Task.Delay(IdleDelay, stoppingToken);
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
                    await Task.Delay(IdleDelay, stoppingToken);
                }
            }
        }
    }
}
