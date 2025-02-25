// wwwroot/js/modules/categoryManager.js
const categoryManager = (function() {
    let oldCategoryValue = "";
    
    /**
     * Başlangıç değerlerini ayarlar
     */
    function initialize() {
        const categorySelect = document.getElementById('selectedCategory');
        if (categorySelect) {
            oldCategoryValue = categorySelect.value;
        }
    }
    
    /**
     * Kategori değiştiğinde çağrılır
     */
    function onChange() {
        const categorySelect = document.getElementById('selectedCategory');
        const hiddenSelectedSubCategory = document.getElementById('hiddenSelectedSubCategory');
        const subCategoryContainer = document.getElementById('subCategoryContainer');
        
        oldCategoryValue = categorySelect.value;
        hiddenSelectedSubCategory.value = "";
        subCategoryContainer.style.display = categorySelect.value ? 'block' : 'none';

        api.getContentItems(function () {
            document.getElementById('editCategoryBtn').style.display =
                categorySelect.value ? 'block' : 'none';

            subcategoryManager.populateSubCategories(categorySelect.value);
            document.getElementById('selectedSubCategory').value = "";
            document.getElementById('editSubCategoryBtn').style.display = 'none';
            document.getElementById('contentDiv').style.display = 'none';
            document.getElementById('submitContentBtn').style.display = 'none';
        });
    }
    
    /**
     * Kategori düzenleme alanını açıp kapatır
     */
    function toggleEdit() {
        var editDiv = document.getElementById('editCategoryDiv');
        if (!editDiv.style.display || editDiv.style.display === 'none') {
            document.getElementById('editedCategory').value = document.getElementById('selectedCategory').value;
            editDiv.style.display = 'block';
        } else {
            editDiv.style.display = 'none';
        }
    }
    
    /**
     * Kategoriyi kaydetmek için çağrılır
     */
    async function saveEdited() {
        const categorySelect = document.getElementById('selectedCategory');
        var newCategory = document.getElementById('editedCategory').value.trim();
        if (!newCategory) {
            notifications.show("Kategori ismi boş olamaz.", "warning");
            return;
        }

        const response = await api.updateCategory(oldCategoryValue, newCategory);
        if (response && response.success) {
            // Seçili option'ı günceller
            for (var i = 0; i < categorySelect.options.length; i++) {
                if (categorySelect.options[i].value === categorySelect.value) {
                    categorySelect.options[i].text = newCategory;
                    categorySelect.options[i].value = newCategory;
                    break;
                }
            }

            // Kategori select değerini yeni isimle günceller
            categorySelect.value = newCategory;
            document.getElementById('editCategoryDiv').style.display = 'none';
            oldCategoryValue = newCategory;

            // Kategoriler güncellendikten sonra tekrar alt kategorileri çek
            api.getContentItems(function () {
                subcategoryManager.populateSubCategories(newCategory);
            });

            notifications.show(response.message || "Kategori başarıyla güncellendi.", "success");
        }
    }

    /**
     * Kategori silme işlemine başlamak için kullanıcıdan onay ister
     */
    function confirmDelete() {
        var category = document.getElementById('deleteCategorySelect').value;
        if (!category) {
            notifications.show("Lütfen silinecek kategoriyi seçiniz.", "warning");
            return;
        }
        document.getElementById('inlineDeleteConfirm').style.display = 'block';
    }

    /**
     * Kategori silme onay penceresini iptal eder
     */
    function cancelDelete() {
        document.getElementById('inlineDeleteConfirm').style.display = 'none';
    }

    /**
     * Kategori silme işlemini sunucuya bildirir
     */
    async function deleteSelected() {
        var select = document.getElementById('deleteCategorySelect');
        var category = select.value;
        document.getElementById('inlineDeleteConfirm').style.display = 'none';

        const response = await api.deleteCategory(category);
        if (response && response.success) {
            notifications.show(response.message || "Kategori başarıyla silindi.", "success");
            setTimeout(function () {
                location.reload();
            }, 2000);
        }
    }
    
    return {
        initialize,
        onChange,
        toggleEdit,
        saveEdited,
        confirmDelete,
        cancelDelete,
        deleteSelected,
        get oldValue() { return oldCategoryValue; }
    };
})();