using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using UglyToad.PdfPig;

namespace Document_Management.Service
{
    public sealed class LocalOcrFailedException : Exception
    {
        public LocalOcrFailedException(string message)
            : base(message)
        {
        }

        public LocalOcrFailedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    public interface IPdfTextExtractionService
    {
        Task<string> ExtractTextAsync(byte[] fileBytes, string contentType, CancellationToken cancellationToken);
    }

    public sealed class PdfTextExtractionService : IPdfTextExtractionService
    {
        private const int MaxExtractedTextLength = 200000;
        private const int MinimumNativeTextLength = 80;
        private readonly ILogger<PdfTextExtractionService> _logger;
        private readonly IConfiguration _configuration;

        public PdfTextExtractionService(
            ILogger<PdfTextExtractionService> logger,
            IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<string> ExtractTextAsync(byte[] fileBytes, string contentType, CancellationToken cancellationToken)
        {
            if (fileBytes.Length == 0)
            {
                return string.Empty;
            }

            var nativeText = ExtractNativeText(fileBytes);
            if (nativeText.Length >= MinimumNativeTextLength)
            {
                return nativeText;
            }

            if (!string.Equals(contentType, "application/pdf", StringComparison.OrdinalIgnoreCase))
            {
                return nativeText;
            }

            var ocrText = await ExtractWithLocalOcrAsync(fileBytes, cancellationToken);
            return string.IsNullOrWhiteSpace(ocrText) ? nativeText : ocrText;
        }

        private string ExtractNativeText(byte[] fileBytes)
        {
            try
            {
                using var memoryStream = new MemoryStream(fileBytes);
                using var document = PdfDocument.Open(memoryStream);
                var builder = new StringBuilder();

                foreach (var page in document.GetPages())
                {
                    var text = page.Text;
                    if (string.IsNullOrWhiteSpace(text))
                    {
                        continue;
                    }

                    if (builder.Length > 0)
                    {
                        builder.Append(' ');
                    }

                    builder.Append(text);

                    if (builder.Length >= MaxExtractedTextLength)
                    {
                        break;
                    }
                }

                return Normalize(builder.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to extract native PDF text from PDF bytes with size {FileSize}", fileBytes.Length);
                return string.Empty;
            }
        }

        private async Task<string> ExtractWithLocalOcrAsync(byte[] fileBytes, CancellationToken cancellationToken)
        {
            var command = _configuration["LocalOcr:Command"];
            var language = _configuration["LocalOcr:Language"];
            var timeout = _configuration.GetValue("LocalOcr:TimeoutSeconds", 180);
            var tesseractPath = _configuration["LocalOcr:TesseractPath"];
            var ghostscriptPath = _configuration["LocalOcr:GhostscriptPath"];

            try
            {
                var tempDirectory = Path.Combine(Path.GetTempPath(), "dms-ocr", Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(tempDirectory);

                var inputPath = Path.Combine(tempDirectory, "input.pdf");
                var outputPath = Path.Combine(tempDirectory, "output.pdf");
                var sidecarPath = Path.Combine(tempDirectory, "output.txt");

                await File.WriteAllBytesAsync(inputPath, fileBytes, cancellationToken);

                try
                {
                    var processStartInfo = new ProcessStartInfo
                    {
                        FileName = string.IsNullOrWhiteSpace(command) ? "ocrmypdf" : command,
                        RedirectStandardError = true,
                        RedirectStandardOutput = true,
                        RedirectStandardInput = false,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    var processPath = processStartInfo.Environment.ContainsKey("PATH")
                        ? processStartInfo.Environment["PATH"]
                        : Environment.GetEnvironmentVariable("PATH");
                    processStartInfo.Environment["PATH"] = BuildProcessPath(processPath, tesseractPath, ghostscriptPath);

                    processStartInfo.ArgumentList.Add("--skip-text");
                    processStartInfo.ArgumentList.Add("--sidecar");
                    processStartInfo.ArgumentList.Add(sidecarPath);

                    if (!string.IsNullOrWhiteSpace(language))
                    {
                        processStartInfo.ArgumentList.Add("--language");
                        processStartInfo.ArgumentList.Add(language);
                    }

                    processStartInfo.ArgumentList.Add(inputPath);
                    processStartInfo.ArgumentList.Add(outputPath);

                    using var process = new Process { StartInfo = processStartInfo };
                    process.Start();

                    using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeout));

                    var standardOutputTask = process.StandardOutput.ReadToEndAsync(timeoutCts.Token);
                    var standardErrorTask = process.StandardError.ReadToEndAsync(timeoutCts.Token);
                    var waitForExitTask = process.WaitForExitAsync(timeoutCts.Token);

                    try
                    {
                        await Task.WhenAll(waitForExitTask, standardOutputTask, standardErrorTask);
                    }
                    catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                    {
                        TryKillProcess(process);
                        throw;
                    }

                    var standardOutput = await standardOutputTask;
                    var standardError = await standardErrorTask;

                    if (process.ExitCode != 0)
                    {
                        _logger.LogWarning(
                            "Local OCR command failed with exit code {ExitCode}. Stdout: {Stdout}. Stderr: {Stderr}",
                            process.ExitCode,
                            standardOutput,
                            standardError);
                        throw new LocalOcrFailedException($"Local OCR command failed with exit code {process.ExitCode}.");
                    }

                    if (!File.Exists(sidecarPath))
                    {
                        _logger.LogWarning("Local OCR command completed but did not create a sidecar text file.");
                        throw new LocalOcrFailedException("Local OCR command completed without creating a sidecar text file.");
                    }

                    var sidecarText = await File.ReadAllTextAsync(sidecarPath, cancellationToken);
                    return Normalize(sidecarText);
                }
                finally
                {
                    TryDeleteDirectory(tempDirectory);
                }
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Local OCR command timed out after {TimeoutSeconds} seconds.", timeout);
                throw new LocalOcrFailedException($"Local OCR command timed out after {timeout} seconds.");
            }
            catch (Exception ex) when (ex is InvalidOperationException or Win32Exception or IOException)
            {
                _logger.LogWarning(ex, "Local OCR command is unavailable or failed to start.");
                throw new LocalOcrFailedException("Local OCR command is unavailable or failed to start.", ex);
            }
        }

        private static string Normalize(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            var builder = new StringBuilder(text.Length);
            var previousWasWhitespace = false;

            foreach (var ch in text)
            {
                if (char.IsWhiteSpace(ch))
                {
                    if (previousWasWhitespace)
                    {
                        continue;
                    }

                    builder.Append(' ');
                    previousWasWhitespace = true;
                }
                else
                {
                    builder.Append(ch);
                    previousWasWhitespace = false;
                }

                if (builder.Length >= MaxExtractedTextLength)
                {
                    break;
                }
            }

            return builder.ToString().Trim();
        }

        private void TryDeleteDirectory(string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, recursive: true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to clean OCR temp directory {TempDirectory}", path);
            }
        }

        private void TryKillProcess(Process process)
        {
            try
            {
                if (process.HasExited)
                {
                    return;
                }

                process.Kill(entireProcessTree: true);
                process.WaitForExit();
            }
            catch (Exception ex) when (ex is InvalidOperationException or NotSupportedException or Win32Exception)
            {
                _logger.LogDebug(ex, "Failed to terminate OCR process {ProcessId}", process.Id);
            }
        }

        private static string BuildProcessPath(string? existingPath, params string?[] extraDirectories)
        {
            var pathParts = new List<string>();

            foreach (var directory in extraDirectories)
            {
                if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
                {
                    pathParts.Add(directory);
                }
            }

            if (!string.IsNullOrWhiteSpace(existingPath))
            {
                pathParts.Add(existingPath);
            }

            return string.Join(Path.PathSeparator, pathParts);
        }
    }
}
