using System.Threading.Tasks;

namespace TestKB.Services.Interfaces
{
    /// <summary>
    /// Veri değişikliklerini dış sistemlere bildirmek için kullanılan servis arayüzü.
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// İçerik dosyası değiştiğinde dış sistemlere bildirim gönderir.
        /// </summary>
        /// <returns>Bildirim başarıyla gönderildiyse true, aksi halde false</returns>
        Task<bool> NotifyContentChangeAsync();
        // Add this to your INotificationService.cs file

        /// <summary>
        /// Python servisinin çalışıp çalışmadığını kontrol eder.
        /// </summary>
        /// <returns>Servis çalışıyorsa true, aksi halde false</returns>
        Task<bool> CheckServiceAvailabilityAsync() => Task.FromResult(false);
    }
}