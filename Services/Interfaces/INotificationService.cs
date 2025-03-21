using System.Threading.Tasks;
using TestKB.Services.Notification;

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

        /// <summary>
        /// Python servisinin çalışıp çalışmadığını kontrol eder.
        /// </summary>
        /// <returns>Servis çalışıyorsa true, aksi halde false</returns>
        Task<bool> CheckServiceAvailabilityAsync();

        /// <summary>
        /// Son bildirim denemesi hakkında ayrıntılı bilgi sağlar.
        /// </summary>
        NotificationResult LastNotificationResult { get; }

        /// <summary>
        /// Son sağlık kontrolü hakkında tanılama bilgisi sağlar.
        /// </summary>
        string LastDiagnosticResult { get; }
    }
}