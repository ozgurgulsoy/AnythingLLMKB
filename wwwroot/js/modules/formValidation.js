// wwwroot/js/modules/formValidation.js
const formValidation = (function() {
    /**
     * Form doğrulama işlevlerini başlatır
     */
    function initialize() {
        // Form validation for new content
        const newContentForm = document.getElementById('newContentForm');
        if (newContentForm) {
            newContentForm.addEventListener('submit', validateNewContentForm);
        }

        // Validation for extend content form
        const extendContentForm = document.getElementById('extendContentForm');
        if (extendContentForm) {
            extendContentForm.addEventListener('submit', validateExtendContentForm);
        }
    }
    
    /**
     * Yeni içerik formunu doğrular
     * @param {Event} event - Form submit olayı
     */
    function validateNewContentForm(event) {
        let hasErrors = false;
        let errorMessages = [];

        // Validate Category
        const newCategory = document.getElementById('newCategory').value.trim();
        if (!newCategory) {
            errorMessages.push('Kategori ismi boş olamaz.');
            hasErrors = true;
        }

        // Validate SubCategory
        const newSubCategory = document.getElementById('newSubCategory').value.trim();
        if (!newSubCategory) {
            errorMessages.push('Alt kategori ismi boş olamaz.');
            hasErrors = true;
        }

        // Validate Content
        const newContent = document.getElementById('newContent').value.trim();
        if (!newContent) {
            errorMessages.push('İçerik boş olamaz.');
            hasErrors = true;
        }

        // Check for duplicate categories (using the allItems array)
        if (newCategory && allItems && Array.isArray(allItems)) {
            const categoryExists = allItems.some(item =>
                item.category && item.category.toLowerCase() === newCategory.toLowerCase());

            if (categoryExists) {
                errorMessages.push(`"${newCategory}" kategorisi zaten mevcut.`);
                hasErrors = true;
            }

            // Check for duplicate subcategories within the same category
            if (newSubCategory && categoryExists) {
                const subcategoryExists = allItems.some(item =>
                    item.category && item.category.toLowerCase() === newCategory.toLowerCase() &&
                    item.subCategory && item.subCategory.toLowerCase() === newSubCategory.toLowerCase());

                if (subcategoryExists) {
                    errorMessages.push(`"${newCategory}" kategorisinde "${newSubCategory}" alt kategorisi zaten mevcut.`);
                    hasErrors = true;
                }
            }
        }

        if (hasErrors) {
            event.preventDefault(); // Prevent form submission
            notifications.show(errorMessages.join('<br>'), 'warning');
        }
    }
    
    /**
     * İçerik genişletme formunu doğrular
     * @param {Event} event - Form submit olayı
     */
    function validateExtendContentForm(event) {
        let hasErrors = false;
        let errorMessages = [];

        const categorySelect = document.getElementById('selectedCategory');
        const subCategorySelect = document.getElementById('selectedSubCategory');
        const extendContentBox = document.getElementById('extendContent');

        // Validate Category selection
        if (!categorySelect.value) {
            errorMessages.push('Lütfen bir kategori seçiniz.');
            hasErrors = true;
        }

        // Validate SubCategory selection
        if (!subCategorySelect.value) {
            errorMessages.push('Lütfen bir alt kategori seçiniz.');
            hasErrors = true;
        }

        // Validate Content
        if (extendContentBox && !extendContentBox.value.trim()) {
            errorMessages.push('İçerik boş olamaz.');
            hasErrors = true;
        }

        if (hasErrors) {
            event.preventDefault(); // Prevent form submission
            notifications.show(errorMessages.join('<br>'), 'warning');
        }
    }
    
    return {
        initialize
    };
})();