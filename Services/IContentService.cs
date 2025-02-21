using TestKB.Models;
using System.Collections.Generic;

namespace TestKB.Services
{
    public interface IContentService
    {
        List<ContentItem> GetContentItems(bool forceReload = false);
        void AddNewContent(string category, string subCategory, string content);
        void ExtendContent(string selectedCategory, string selectedSubCategory, string newSubCategory, string content);
        void UpdateContentItems(List<ContentItem> items);
    }
}
