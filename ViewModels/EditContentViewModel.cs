namespace TestKB.ViewModels
{
    public class EditContentViewModel
    {
        public List<string> ExistingCategories { get; set; }
        public Dictionary<string, List<string>> ExistingSubCategories { get; set; }

        // For "New Content" form
        public NewContentModel NewContent { get; set; }

        // For "Extend Content" form
        public ExtendContentModel ExtendContent { get; set; }
    }

}
