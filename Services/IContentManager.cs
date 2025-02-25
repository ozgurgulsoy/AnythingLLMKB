using System.Collections.Generic;
using TestKB.Models;
using TestKB.ViewModels;

namespace TestKB.Services
{
    public interface IContentManager
    {
        ContentListViewModel BuildContentListViewModel(string category);
        EditContentViewModel BuildEditContentViewModel(NewContentViewModel newContent, ExtendContentViewModel extendContent);
        ExtendContentViewModel CreateExtendContentViewModel(string selectedCategory, string selectedSubCategory);
        void AddNewContent(NewContentViewModel model);
        void UpdateContent(ExtendContentViewModel model);
        void UpdateCategory(string oldCategory, string newCategory);
        void UpdateSubCategory(string category, string oldSubCategory, string newSubCategory);
        void AddSubCategory(string category, string newSubCategory);
        int DeleteCategory(string category);
        List<ContentItem> GetAllContentItems();
    }
}
