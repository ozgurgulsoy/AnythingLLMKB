using System.ComponentModel.DataAnnotations;

namespace TestKB.Models.ViewModels
{
    public class ExtendContentViewModel
    {
        [Required(ErrorMessage = "Lütfen bir kategori seçiniz.")]
        public string? SelectedCategory { get; set; }

        // Not marked as required so the user can leave it empty if a default is used
        public string? SelectedSubCategory { get; set; }

        // Optional – remove any [Required] attribute if present.
        public string? NewSubCategory { get; set; }

        [Required(ErrorMessage = "İçerik boş bırakılamaz.")]
        public string Content { get; set; }

        // Change these to nullable to avoid automatic "required" errors
        public string? EditedCategory { get; set; }
        public string? EditedSubCategory { get; set; }
    }
}
