using TestKB.Models;

namespace TestKB.Services
{
    public interface IContentService
    {
        List<ContentItem> GetContentItems();
        void AddNewContent(string category, string subCategory, string content);
        void ExtendContent(string selectedCategory, string selectedSubCategory, string newSubCategory, string content);
        public void UpdateContentItems(List<ContentItem> items);
    }

}
