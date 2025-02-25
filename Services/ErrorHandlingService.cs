using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace TestKB.Services
{
    /// <summary>
    /// Uygulama genelinde hata yönetimi için merkezi servis.
    /// </summary>
    public class ErrorHandlingService : IErrorHandlingService
    {
        private readonly ILogger<ErrorHandlingService> _logger;

        public ErrorHandlingService(ILogger<ErrorHandlingService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Uygulama hatalarını loglama ve yönetme işlemini gerçekleştirir.
        /// </summary>
        /// <param name="ex">İşlenen istisna</param>
        /// <param name="context">Hatanın oluştuğu bağlam bilgisi</param>
        /// <returns>Kullanıcıya gösterilecek hata mesajı</returns>
        // ErrorHandlingService.cs iyileştirmesi - Pattern matching ile
        public ErrorResponse HandleException(Exception ex, string context)
        {
            _logger.LogError(ex, $"Hata oluştu: {context}");

            return ex switch
            {
                ArgumentNullException or ArgumentException => new ErrorResponse
                {
                    Message = "Geçersiz parametre veya değer.",
                    ErrorCode = ErrorCode.InvalidArgument,
                    Success = false
                },
                InvalidOperationException => new ErrorResponse
                {
                    Message = ex.Message,
                    ErrorCode = ErrorCode.InvalidOperation,
                    Success = false
                },
                KeyNotFoundException => new ErrorResponse
                {
                    Message = "İstenen öğe bulunamadı.",
                    ErrorCode = ErrorCode.NotFound,
                    Success = false
                },
                _ => new ErrorResponse
                {
                    Message = "İşlem sırasında bir hata oluştu.",
                    ErrorCode = ErrorCode.GeneralError,
                    Success = false
                }
            };
        }

        /// <summary>
        /// Model doğrulama hatalarını işleyip formatlar.
        /// </summary>
        /// <param name="modelErrors">Model doğrulama hataları</param>
        /// <returns>Kullanıcıya gösterilecek hata mesajı</returns>
        public ErrorResponse HandleValidationErrors(IEnumerable<string> modelErrors)
        {
            var errorMessages = string.Join("; ", modelErrors);
            _logger.LogWarning("Doğrulama hataları: {Errors}", errorMessages);

            return new ErrorResponse
            {
                Message = "Girilen bilgilerde hatalar var.",
                ErrorCode = ErrorCode.ValidationError,
                ValidationErrors = modelErrors,
                Success = false
            };
        }

        /// <summary>
        /// Başarılı işlem sonucunu oluşturur.
        /// </summary>
        /// <param name="message">Kullanıcıya gösterilecek başarı mesajı</param>
        /// <returns>Başarı durumu nesnesi</returns>
        public ErrorResponse CreateSuccessResponse(string message = "İşlem başarıyla tamamlandı.")
        {
            return new ErrorResponse
            {
                Message = message,
                Success = true
            };
        }
    }
}