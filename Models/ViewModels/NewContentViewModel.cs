using System.ComponentModel.DataAnnotations;

namespace TestKB.Models.ViewModels
{
    public class NewContentViewModel
    {
        [Required(ErrorMessage = "Kategori boş bırakılamaz.")]

        public string Category { get; set; }
        [Required(ErrorMessage = "Alt Kategori boş bırakılamaz.")]

        public string SubCategory { get; set; }
        [Required(ErrorMessage = "İçerik boş bırakılamaz.")]
        public string Content { get; set; }
    }
}
