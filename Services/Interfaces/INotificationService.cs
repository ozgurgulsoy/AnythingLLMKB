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
    }
}