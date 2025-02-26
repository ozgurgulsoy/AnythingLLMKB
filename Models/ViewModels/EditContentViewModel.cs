namespace TestKB.Models.ViewModels
{
    public class EditContentViewModel
    {
        public List<string> ExistingCategories { get; set; }
        public Dictionary<string, List<string>> ExistingSubCategories { get; set; }

        // For "New Content" form
        public NewContentViewModel NewContent { get; set; }

        // For "Extend Content" form
        public ExtendContentViewModel ExtendContent { get; set; }

        public List<ContentItem> ContentItems { get; set; }
    }

}
