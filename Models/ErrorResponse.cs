using System.Collections.Generic;

namespace TestKB.Services
{
    /// <summary>
    /// Hata yanıt modeli.
    /// </summary>
    public class ErrorResponse
    {
        /// <summary>
        /// İşlem başarılı mı
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Kullanıcıya gösterilecek hata mesajı
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Hata kodu
        /// </summary>
        public ErrorCode ErrorCode { get; set; } = ErrorCode.None;

        /// <summary>
        /// Doğrulama hatalarının listesi (varsa)
        /// </summary>
        public IEnumerable<string> ValidationErrors { get; set; } = new List<string>();
    }

    /// <summary>
    /// Hata kodları enum'u
    /// </summary>
    public enum ErrorCode
    {
        None = 0,
        GeneralError = 1,
        ValidationError = 2,
        InvalidArgument = 3,
        InvalidOperation = 4,
        NotFound = 5,
        Unauthorized = 6,
        Forbidden = 7
    }
}