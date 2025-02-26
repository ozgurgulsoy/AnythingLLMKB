namespace TestKB.Models.ViewModels
{
    public class ContentListViewModel
    {
        public IEnumerable<ContentItem> ContentItems { get; set; }
        public IEnumerable<string> AllCategories { get; set; }
        public string SelectedCategory { get; set; }
    }
}
