// wwwroot/js/main.js
document.addEventListener('DOMContentLoaded', function () {
    // Global variable for content items
    window.allItems = allItemsJsonData;
    
    // Initialize all modules
    categoryManager.initialize();
    subcategoryManager.initialize();
    formValidation.initialize();
    
    // Expose public methods to window for HTML event handlers
    window.showNotification = notifications.show;
    window.refreshAllItems = api.getContentItems;
    
    // Category management
    window.onCategoryChange = categoryManager.onChange;
    window.toggleEditCategory = categoryManager.toggleEdit;
    window.saveEditedCategory = categoryManager.saveEdited;
    window.confirmDeleteCategory = categoryManager.confirmDelete;
    window.cancelDelete = categoryManager.cancelDelete;
    window.deleteCategory = categoryManager.deleteSelected;
    
    // Subcategory management
    window.populateSubCategories = subcategoryManager.populateSubCategories;
    window.onSubCategoryChange = subcategoryManager.onChange;
    window.toggleEditSubCategory = subcategoryManager.toggleEdit;
    window.saveEditedSubCategory = subcategoryManager.saveEdited;
    window.toggleAddSubCategory = subcategoryManager.toggleAdd;
    window.saveNewSubCategory = subcategoryManager.saveNew;
    
    // Initialize with any existing data
    if (document.getElementById('selectedCategory') && document.getElementById('selectedCategory').value) {
        api.getContentItems(function () {
            document.getElementById('subCategoryContainer').style.display = 'block';
            subcategoryManager.populateSubCategories(document.getElementById('selectedCategory').value);

            // Daha önce seçili olan alt kategori varsa onu tekrar seç
            var preselectedSub = document.getElementById('hiddenSelectedSubCategory').value;
            if (preselectedSub) {
                document.getElementById('selectedSubCategory').value = preselectedSub;
                subcategoryManager.onChange();
            }
        });
    }
    
    // Display server-side messages if they exist
    if (typeof tempData !== 'undefined') {
        if (tempData.errorMessage) {
            notifications.show(tempData.errorMessage, 'danger');
        }

        if (tempData.successMessage) {
            notifications.show(tempData.successMessage, 'success');
        }
    }
    
    // Tab change event handler
    $('a[data-toggle="tab"]').on('shown.bs.tab', function (e) {
        var target = $(e.target).attr("href");
        // "Var Olan İçeriği Düzenle" sekmesine geçiliyorsa
        if (target === "#extendContentSection") {
            if (document.getElementById('selectedCategory') && document.getElementById('selectedCategory').value) {
                api.getContentItems(function () {
                    document.getElementById('subCategoryContainer').style.display = 'block';
                    subcategoryManager.populateSubCategories(document.getElementById('selectedCategory').value);

                    // Önceden seçilmiş alt kategori varsa onu seçip onChange çağır
                    var preselectedSub = document.getElementById('hiddenSelectedSubCategory').value;
                    if (preselectedSub) {
                        document.getElementById('selectedSubCategory').value = preselectedSub;
                        subcategoryManager.onChange();
                    }
                });
            }
        }
    });
});