// Services/MemoryCacheService.cs
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace TestKB.Services
{
    /// <summary>
    /// Memory cache kullanarak önbellek işlemlerini yöneten servis implementasyonu.
    /// </summary>
    public class MemoryCacheService : ICacheService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<MemoryCacheService> _logger;
        private readonly ConcurrentDictionary<string, bool> _cacheKeys;

        public MemoryCacheService(
            IMemoryCache memoryCache,
            ILogger<MemoryCacheService> logger)
        {
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cacheKeys = new ConcurrentDictionary<string, bool>();
        }

        /// <summary>
        /// Önbellekten veri alır veya yoksa sağlanan delegate fonksiyonu ile üretilip önbelleğe kaydedilir.
        /// </summary>
        public T GetOrSet<T>(string key, Func<T> factory, TimeSpan? expiration = null)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Önbellek anahtarı boş olamaz", nameof(key));

            if (_memoryCache.TryGetValue(key, out T cachedValue))
            {
                _logger.LogDebug("Önbellekten veri alındı. Anahtar: {CacheKey}", key);
                return cachedValue;
            }

            T newValue = factory();
            var cacheOptions = new MemoryCacheEntryOptions();

            if (expiration.HasValue)
            {
                cacheOptions.SetAbsoluteExpiration(expiration.Value);
            }
            else
            {
                // Varsayılan olarak 30 dakika
                cacheOptions.SetAbsoluteExpiration(TimeSpan.FromMinutes(30));
            }

            cacheOptions.RegisterPostEvictionCallback((evictedKey, _, _, _) =>
            {
                _cacheKeys.TryRemove(evictedKey.ToString(), out _);
            });

            _memoryCache.Set(key, newValue, cacheOptions);
            _cacheKeys.TryAdd(key, true);
            _logger.LogDebug("Veri önbelleğe kaydedildi. Anahtar: {CacheKey}", key);

            return newValue;
        }

        /// <summary>
        /// Önbellekten veri alır veya yoksa sağlanan asenkron delegate fonksiyonu ile üretilip önbelleğe kaydedilir.
        /// </summary>
        public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Önbellek anahtarı boş olamaz", nameof(key));

            if (_memoryCache.TryGetValue(key, out T cachedValue))
            {
                _logger.LogDebug("Önbellekten veri alındı. Anahtar: {CacheKey}", key);
                return cachedValue;
            }

            T newValue = await factory();
            var cacheOptions = new MemoryCacheEntryOptions();

            if (expiration.HasValue)
            {
                cacheOptions.SetAbsoluteExpiration(expiration.Value);
            }
            else
            {
                // Varsayılan olarak 30 dakika
                cacheOptions.SetAbsoluteExpiration(TimeSpan.FromMinutes(30));
            }

            cacheOptions.RegisterPostEvictionCallback((evictedKey, _, _, _) =>
            {
                _cacheKeys.TryRemove(evictedKey.ToString(), out _);
            });

            _memoryCache.Set(key, newValue, cacheOptions);
            _cacheKeys.TryAdd(key, true);
            _logger.LogDebug("Veri önbelleğe kaydedildi. Anahtar: {CacheKey}", key);

            return newValue;
        }

        /// <summary>
        /// Belirtilen anahtarla ilişkili veriyi önbellekten kaldırır.
        /// </summary>
        public void Remove(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return;

            _memoryCache.Remove(key);
            _cacheKeys.TryRemove(key, out _);
            _logger.LogDebug("Veri önbellekten kaldırıldı. Anahtar: {CacheKey}", key);
        }

        /// <summary>
        /// Belirtilen önekle başlayan tüm anahtarları önbellekten kaldırır.
        /// </summary>
        public void RemoveByPrefix(string keyPrefix)
        {
            if (string.IsNullOrWhiteSpace(keyPrefix))
                return;

            var keysToRemove = _cacheKeys.Keys.Where(k => k.StartsWith(keyPrefix, StringComparison.OrdinalIgnoreCase)).ToList();

            foreach (var key in keysToRemove)
            {
                _memoryCache.Remove(key);
                _cacheKeys.TryRemove(key, out _);
            }

            _logger.LogDebug("Önbellekten {Count} anahtar kaldırıldı. Önek: {KeyPrefix}", keysToRemove.Count, keyPrefix);
        }
    }
}