// Services/ICacheService.cs
using System;

namespace TestKB.Services
{
    /// <summary>
    /// Uygulama genelinde önbellek işlemlerini yönetmek için servis arayüzü.
    /// </summary>
    public interface ICacheService
    {
        /// <summary>
        /// Önbellekten veri alır veya yoksa sağlanan delegate fonksiyonu ile üretilip önbelleğe kaydedilir.
        /// </summary>
        /// <typeparam name="T">Veri tipi</typeparam>
        /// <param name="key">Önbellek anahtarı</param>
        /// <param name="factory">Veriyi üreten fonksiyon</param>
        /// <param name="expiration">İsteğe bağlı geçerlilik süresi</param>
        /// <returns>Önbellekteki veya yeni üretilen veri</returns>
        T GetOrSet<T>(string key, Func<T> factory, TimeSpan? expiration = null);

        /// <summary>
        /// Önbellekten veri alır veya yoksa sağlanan asenkron delegate fonksiyonu ile üretilip önbelleğe kaydedilir.
        /// </summary>
        /// <typeparam name="T">Veri tipi</typeparam>
        /// <param name="key">Önbellek anahtarı</param>
        /// <param name="factory">Veriyi asenkron üreten fonksiyon</param>
        /// <param name="expiration">İsteğe bağlı geçerlilik süresi</param>
        /// <returns>Önbellekteki veya yeni üretilen veri</returns>
        Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null);

        /// <summary>
        /// Belirtilen anahtarla ilişkili veriyi önbellekten kaldırır.
        /// </summary>
        /// <param name="key">Kaldırılacak verinin önbellek anahtarı</param>
        void Remove(string key);

        /// <summary>
        /// Belirtilen önekle başlayan tüm anahtarları önbellekten kaldırır.
        /// </summary>
        /// <param name="keyPrefix">Önbellek anahtarı öneki</param>
        void RemoveByPrefix(string keyPrefix);
    }
}