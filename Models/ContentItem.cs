namespace TestKB.Models
{
    public class ContentItem
    {
        public string Category { get; set; }
        public string SubCategory { get; set; }
        public string Content { get; set; }
        
        public Department Department { get; set; }
    }
}