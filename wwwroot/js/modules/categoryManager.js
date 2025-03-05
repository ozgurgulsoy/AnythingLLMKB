// wwwroot/js/modules/categoryManager.js
const categoryManager = (function () {
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
        const editCategoryBtn = document.getElementById('editCategoryBtn');

        if (!categorySelect) {
            console.error('Kategori seçim elementi bulunamadı');
            return;
        }

        oldCategoryValue = categorySelect.value;

        if (hiddenSelectedSubCategory) {
            hiddenSelectedSubCategory.value = "";
        }

        if (subCategoryContainer) {
            subCategoryContainer.style.display = categorySelect.value ? 'block' : 'none';
        }

        api.getContentItems(function () {
            if (editCategoryBtn) {
                editCategoryBtn.style.display = categorySelect.value ? 'block' : 'none';
            }

            subcategoryManager.populateSubCategories(categorySelect.value);

            const subCategorySelect = document.getElementById('selectedSubCategory');
            if (subCategorySelect) {
                subCategorySelect.value = "";
            }

            const editSubCategoryBtn = document.getElementById('editSubCategoryBtn');
            if (editSubCategoryBtn) {
                editSubCategoryBtn.style.display = 'none';
            }

            const contentDiv = document.getElementById('contentDiv');
            if (contentDiv) {
                contentDiv.style.display = 'none';
            }

            const submitContentBtn = document.getElementById('submitContentBtn');
            if (submitContentBtn) {
                submitContentBtn.style.display = 'none';
            }
        });
    }

    /**
     * Kategori düzenleme alanını açıp kapatır
     */
    function toggleEdit() {
        const editCategoryDiv = document.getElementById('editCategoryDiv');
        const categorySelect = document.getElementById('selectedCategory');
        const editedCategory = document.getElementById('editedCategory');

        if (!editCategoryDiv || !categorySelect) {
            console.error('Kategori düzenleme elementleri bulunamadı');
            return;
        }

        if (!editCategoryDiv.style.display || editCategoryDiv.style.display === 'none') {
            if (editedCategory) {
                editedCategory.value = categorySelect.value;
            }
            editCategoryDiv.style.display = 'block';
        } else {
            editCategoryDiv.style.display = 'none';
        }
    }

    /**
     * Kategoriyi kaydetmek için çağrılır
     */
    async function saveEdited() {
        const categorySelect = document.getElementById('selectedCategory');
        const editedCategory = document.getElementById('editedCategory');
        const editCategoryDiv = document.getElementById('editCategoryDiv');

        if (!categorySelect || !editedCategory) {
            console.error('Kategori düzenleme elementleri bulunamadı');
            return;
        }

        const newCategory = editedCategory.value.trim();
        if (!newCategory) {
            notifications.show("Kategori ismi boş olamaz.", "warning");
            return;
        }

        // Aynı isim kontrolü
        if (newCategory === oldCategoryValue) {
            notifications.show("Kategori ismi değiştirilmedi.", "info");
            editCategoryDiv.style.display = 'none';
            return;
        }

        // İsim uzunluğu kontrolü - simple validation
        if (newCategory.length < 2) {
            notifications.show("Kategori ismi en az 2 karakter olmalıdır.", "warning");
            return;
        }

        const response = await api.updateCategory(oldCategoryValue, newCategory);
        if (response && response.success) {
            // Seçili option'ı günceller
            for (let i = 0; i < categorySelect.options.length; i++) {
                if (categorySelect.options[i].value === categorySelect.value) {
                    categorySelect.options[i].text = newCategory;
                    categorySelect.options[i].value = newCategory;
                    break;
                }
            }

            // Kategori select değerini yeni isimle günceller
            categorySelect.value = newCategory;

            if (editCategoryDiv) {
                editCategoryDiv.style.display = 'none';
            }

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
        const deleteCategorySelect = document.getElementById('deleteCategorySelect');
        const inlineDeleteConfirm = document.getElementById('inlineDeleteConfirm');

        if (!deleteCategorySelect) {
            console.error('Kategori silme elementi bulunamadı');
            return;
        }

        const category = deleteCategorySelect.value;
        if (!category) {
            notifications.show("Lütfen silinecek kategoriyi seçiniz.", "warning");
            return;
        }

        if (inlineDeleteConfirm) {
            inlineDeleteConfirm.style.display = 'block';
        }
    }

    /**
     * Kategori silme onay penceresini iptal eder
     */
    function cancelDelete() {
        const inlineDeleteConfirm = document.getElementById('inlineDeleteConfirm');

        if (inlineDeleteConfirm) {
            inlineDeleteConfirm.style.display = 'none';
        }
    }

    /**
     * Kategori silme işlemini sunucuya bildirir
     */
    async function deleteSelected() {
        const deleteCategorySelect = document.getElementById('deleteCategorySelect');
        const inlineDeleteConfirm = document.getElementById('inlineDeleteConfirm');

        if (!deleteCategorySelect) {
            console.error('Kategori silme elementi bulunamadı');
            return;
        }

        const category = deleteCategorySelect.value;

        if (inlineDeleteConfirm) {
            inlineDeleteConfirm.style.display = 'none';
        }

        if (!category) {
            notifications.show("Silinecek kategori seçilmedi.", "warning");
            return;
        }

        const response = await api.deleteCategory(category);
        if (response && response.success) {
            notifications.show(response.message || "Kategori başarıyla silindi.", "success");
            setTimeout(function () {
                location.reload();
            }, 2000);
        }
    }

    /**
     * Departmana göre içerik öğelerini filtreler
     * @param {Array} items - Tüm içerik öğeleri
     * @param {number} department - Departman ID'si
     * @returns {Array} Departmana göre filtrelenmiş içerik öğeleri
     */
    function filterItemsByDepartment(items, department) {
        if (!items || !Array.isArray(items) || department === undefined) {
            return items || [];
        }

        return items.filter(function(item) {
            return item.department === department;
        });
    }

    return {
        initialize,
        onChange,
        toggleEdit,
        saveEdited,
        confirmDelete,
        cancelDelete,
        deleteSelected,
        filterItemsByDepartment,
        get oldValue() { return oldCategoryValue; }
    };
})();