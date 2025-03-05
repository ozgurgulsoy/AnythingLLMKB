// Repositories/ContentRepository.cs
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

        public ContentRepository(
            IWebHostEnvironment env,
            ILogger<ContentRepository> logger,
            INotificationService notificationService)
        {
            if (env == null) throw new ArgumentNullException(nameof(env));
            _filePath = Path.Combine(env.WebRootPath, "data.json");
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));

            _jsonOptions = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = true
            };
        }

        /// <summary>
        /// Tüm içerik öğelerini asenkron olarak getirir.
        /// </summary>
        public async Task<List<ContentItem>> GetAllAsync()
        {
            if (!await ExistsAsync())
                return new List<ContentItem>();

            await _semaphore.WaitAsync();
            try
            {
                var jsonString = await File.ReadAllTextAsync(_filePath, Encoding.UTF8);
                return JsonSerializer.Deserialize<List<ContentItem>>(jsonString, _jsonOptions)
                       ?? new List<ContentItem>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "JSON dosyası okunurken hata: {Message}", ex.Message);
                return new List<ContentItem>();
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
                var jsonString = JsonSerializer.Serialize(items, _jsonOptions);
                var directory = Path.GetDirectoryName(_filePath);

                // Ensure directory exists
                if (!Directory.Exists(directory) && directory != null)
                {
                    Directory.CreateDirectory(directory);
                }

                await File.WriteAllTextAsync(_filePath, jsonString, Encoding.UTF8);
                
                // İçerik değişikliğini bildir
                await NotifyContentChanged();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "JSON dosyasına yazılırken hata: {Message}", ex.Message);
                throw; // Re-throw after logging to allow higher level error handling
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
            return Task.FromResult(File.Exists(_filePath));
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