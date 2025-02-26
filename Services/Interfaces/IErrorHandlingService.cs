namespace TestKB.Services.Interfaces
{
    public interface IErrorHandlingService
    {
        /// <summary>
        /// İstisnaları işler ve uygun yanıtı döndürür.
        /// </summary>
        ErrorResponse HandleException(Exception ex, string context);

        /// <summary>
        /// Model doğrulama hatalarını işler.
        /// </summary>
        ErrorResponse HandleValidationErrors(IEnumerable<string> modelErrors);

        /// <summary>
        /// Başarılı işlem sonucunu oluşturur.
        /// </summary>
        ErrorResponse CreateSuccessResponse(string message = "İşlem başarıyla tamamlandı.");
    }
}