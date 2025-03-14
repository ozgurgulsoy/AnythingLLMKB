using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using TestKB.Models;
using TestKB.Services.Interfaces;

namespace TestKB.Repositories
{
    /// <summary>
    /// JSON dosyasını kullanarak içerik öğelerini depolayan repository implementasyonu.
    /// </summary>
    public class ContentRepository : IContentRepository
    {
        private readonly string _filePath;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly ILogger<ContentRepository> _logger;
        private readonly INotificationService _notificationService;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly IWebHostEnvironment _environment;

        public ContentRepository(
            IWebHostEnvironment env,
            ILogger<ContentRepository> logger,
            INotificationService notificationService)
        {
            if (env == null) throw new ArgumentNullException(nameof(env));
            _environment = env;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));

            // Data directory outside of wwwroot to ensure write permissions in production
            string dataDirectory = Path.Combine(env.ContentRootPath, "App_Data");

            // Ensure the directory exists
            EnsureDirectoryExists(dataDirectory);

            _filePath = Path.Combine(dataDirectory, "data.json");

            // If the data file doesn't exist yet but we have a copy in wwwroot, copy it to the new location
            if (!File.Exists(_filePath))
            {
                string wwwrootDataFile = Path.Combine(env.WebRootPath, "data.json");
                if (File.Exists(wwwrootDataFile))
                {
                    try
                    {
                        File.Copy(wwwrootDataFile, _filePath);
                        _logger.LogInformation("Copied initial data from wwwroot to App_Data directory");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to copy initial data file. A new one will be created.");
                    }
                }
            }

            _jsonOptions = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = true
            };

            _logger.LogInformation("ContentRepository initialized with file path: {FilePath}", _filePath);
        }

        /// <summary>
        /// Ensure a directory exists, creating it if necessary
        /// </summary>
        private void EnsureDirectoryExists(string directoryPath)
        {
            try
            {
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                    _logger.LogInformation("Created directory: {DirectoryPath}", directoryPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create directory: {DirectoryPath}", directoryPath);
                throw new InvalidOperationException($"Failed to create directory: {directoryPath}", ex);
            }
        }

        /// <summary>
        /// Tüm içerik öğelerini asenkron olarak getirir.
        /// </summary>
        public async Task<List<ContentItem>> GetAllAsync()
        {
            if (!await ExistsAsync())
            {
                _logger.LogInformation("Data file doesn't exist. Returning empty list.");
                return new List<ContentItem>();
            }

            await _semaphore.WaitAsync();
            try
            {
                _logger.LogDebug("Reading data file from: {FilePath}", _filePath);
                var jsonString = await File.ReadAllTextAsync(_filePath, Encoding.UTF8);
                return JsonSerializer.Deserialize<List<ContentItem>>(jsonString, _jsonOptions)
                       ?? new List<ContentItem>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "JSON dosyası okunurken hata: {FilePath}, {Message}", _filePath, ex.Message);
                throw new InvalidOperationException($"Failed to read data file: {_filePath}", ex);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// İçerik öğelerini asenkron olarak kaydeder.
        /// </summary>
        public async Task SaveAsync(List<ContentItem> items)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));

            await _semaphore.WaitAsync();
            try
            {
                // Ensure the directory exists before attempting to write
                var directory = Path.GetDirectoryName(_filePath);
                if (directory != null)
                {
                    EnsureDirectoryExists(directory);
                }

                var jsonString = JsonSerializer.Serialize(items, _jsonOptions);

                // Write to a temporary file first, then move it to avoid corruption if interrupted
                var tempFilePath = _filePath + ".tmp";

                try
                {
                    _logger.LogDebug("Writing data to temp file: {TempFilePath}", tempFilePath);
                    await File.WriteAllTextAsync(tempFilePath, jsonString, Encoding.UTF8);

                    // Backup the existing file if it exists
                    if (File.Exists(_filePath))
                    {
                        var backupFilePath = _filePath + ".bak";
                        if (File.Exists(backupFilePath))
                        {
                            File.Delete(backupFilePath);
                        }
                        File.Move(_filePath, backupFilePath);
                    }

                    // Move the temp file to the real location
                    File.Move(tempFilePath, _filePath);
                    _logger.LogInformation("Successfully wrote data to: {FilePath}", _filePath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "JSON dosyasına yazılırken hata: {FilePath}, {Message}", _filePath, ex.Message);
                    throw new InvalidOperationException($"Failed to write data file: {_filePath}", ex);
                }
                finally
                {
                    // Clean up temp file if it still exists
                    if (File.Exists(tempFilePath))
                    {
                        try
                        {
                            File.Delete(tempFilePath);
                        }
                        catch
                        {
                            // Just log, don't throw from finally
                            _logger.LogWarning("Failed to delete temporary file: {TempFilePath}", tempFilePath);
                        }
                    }
                }

                // İçerik değişikliğini bildir
                try
                {
                    await NotifyContentChanged();
                }
                catch (Exception ex)
                {
                    // Log but don't fail the save operation due to notification failure
                    _logger.LogWarning(ex, "Failed to notify about content change, but data was saved successfully");
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Depo dosyasının var olup olmadığını kontrol eder.
        /// </summary>
        public Task<bool> ExistsAsync()
        {
            var exists = File.Exists(_filePath);
            _logger.LogDebug("Checking if data file exists: {FilePath}, Result: {Exists}", _filePath, exists);
            return Task.FromResult(exists);
        }

        /// <summary>
        /// İçerik değişikliğini ilgili servislere bildirir.
        /// </summary>
        private async Task NotifyContentChanged()
        {
            try
            {
                bool success = await _notificationService.NotifyContentChangeAsync();
                if (success)
                {
                    _logger.LogInformation("İçerik değişikliği bildirimi başarıyla gönderildi");
                }
                else
                {
                    _logger.LogWarning("İçerik değişikliği bildirimi gönderilemedi");
                }
            }
            catch (Exception ex)
            {
                // Bildirim gönderirken oluşan hata içerik kaydetme işlemini etkilemeyecek şekilde yutulur
                _logger.LogError(ex, "İçerik değişikliği bildirilirken bir hata oluştu");
            }
        }
    }
}