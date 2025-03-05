using System.ComponentModel.DataAnnotations;
using TestKB.Models;

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
        
        [Required(ErrorMessage = "Departman seçimi zorunludur.")]
        public Department Department { get; set; }
    }
}