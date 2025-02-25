// Services/IContentManager.cs
using System.Collections.Generic;
using System.Threading.Tasks;
using TestKB.Models;
using TestKB.ViewModels;

namespace TestKB.Services
{
    public interface IContentManager
    {
        Task<ContentListViewModel> BuildContentListViewModelAsync(string category);
        Task<EditContentViewModel> BuildEditContentViewModelAsync(NewContentViewModel newContent, ExtendContentViewModel extendContent);
        Task<ExtendContentViewModel> CreateExtendContentViewModelAsync(string selectedCategory, string selectedSubCategory);
        Task AddNewContentAsync(NewContentViewModel model);
        Task UpdateContentAsync(ExtendContentViewModel model);
        Task UpdateCategoryAsync(string oldCategory, string newCategory);
        Task UpdateSubCategoryAsync(string category, string oldSubCategory, string newSubCategory);
        Task AddSubCategoryAsync(string category, string newSubCategory);
        Task<int> DeleteCategoryAsync(string category);
        Task<List<ContentItem>> GetAllContentItemsAsync();
    }
}