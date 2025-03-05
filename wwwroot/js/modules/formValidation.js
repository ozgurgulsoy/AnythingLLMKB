// wwwroot/js/modules/formValidation.js
const formValidation = (function () {
    /**
     * Form doğrulama işlevlerini başlatır
     */
    function initialize() {
        // Form validation for new content
        const newContentForm = document.getElementById('newContentForm');
        if (newContentForm) {
            newContentForm.addEventListener('submit', validateNewContentForm);

            // Add basic input validation events
            setupBasicValidation();
        }

        // Validation for extend content form
        const extendContentForm = document.getElementById('extendContentForm');
        if (extendContentForm) {
            extendContentForm.addEventListener('submit', validateExtendContentForm);
        }
    }

    /**
     * Basic input validation to provide feedback on blur
     */
    function setupBasicValidation() {
        // New content form input fields
        const newCategory = document.getElementById('newCategory');
        const newSubCategory = document.getElementById('newSubCategory');
        const newContent = document.getElementById('newContent');

        // Add blur event listeners for basic validation
        if (newCategory) {
            newCategory.addEventListener('blur', function () {
                if (!newCategory.value.trim()) {
                    newCategory.classList.add('is-invalid');
                } else {
                    newCategory.classList.remove('is-invalid');
                }
            });
        }

        if (newSubCategory) {
            newSubCategory.addEventListener('blur', function () {
                if (!newSubCategory.value.trim()) {
                    newSubCategory.classList.add('is-invalid');
                } else {
                    newSubCategory.classList.remove('is-invalid');
                }
            });
        }

        if (newContent) {
            newContent.addEventListener('blur', function () {
                if (!newContent.value.trim()) {
                    newContent.classList.add('is-invalid');
                } else {
                    newContent.classList.remove('is-invalid');
                }
            });
        }
    }

    /**
     * Yeni içerik formunu doğrular
     * @param {Event} event - Form submit olayı
     */
    function validateNewContentForm(event) {
        let hasErrors = false;
        let errorMessages = [];

        // Get form elements
        const newCategory = document.getElementById('newCategory');
        const newSubCategory = document.getElementById('newSubCategory');
        const newContent = document.getElementById('newContent');

        // Clear previous validation state
        if (newCategory) newCategory.classList.remove('is-invalid');
        if (newSubCategory) newSubCategory.classList.remove('is-invalid');
        if (newContent) newContent.classList.remove('is-invalid');

        // Validate Category
        const categoryValue = newCategory ? newCategory.value.trim() : '';
        if (!categoryValue) {
            errorMessages.push('Kategori ismi boş olamaz.');
            if (newCategory) newCategory.classList.add('is-invalid');
            hasErrors = true;
        } else if (categoryValue.length < 2) {
            errorMessages.push('Kategori ismi en az 2 karakter olmalıdır.');
            if (newCategory) newCategory.classList.add('is-invalid');
            hasErrors = true;
        }

        // Validate SubCategory
        const subCategoryValue = newSubCategory ? newSubCategory.value.trim() : '';
        if (!subCategoryValue) {
            errorMessages.push('Alt kategori ismi boş olamaz.');
            if (newSubCategory) newSubCategory.classList.add('is-invalid');
            hasErrors = true;
        } else if (subCategoryValue.length < 2) {
            errorMessages.push('Alt kategori ismi en az 2 karakter olmalıdır.');
            if (newSubCategory) newSubCategory.classList.add('is-invalid');
            hasErrors = true;
        }

        // Validate Content
        const contentValue = newContent ? newContent.value.trim() : '';
        if (!contentValue) {
            errorMessages.push('İçerik boş olamaz.');
            if (newContent) newContent.classList.add('is-invalid');
            hasErrors = true;
        }

        // Check for duplicate categories (using the allItems array)
        if (categoryValue && window.allItems && Array.isArray(window.allItems)) {
            const categoryExists = window.allItems.some(function (item) {
                return item.category && item.category.toLowerCase() === categoryValue.toLowerCase();
            });

            if (categoryExists) {
                errorMessages.push(`"${categoryValue}" kategorisi zaten mevcut.`);
                if (newCategory) newCategory.classList.add('is-invalid');
                hasErrors = true;
            }

            // Check for duplicate subcategories within the same category
            if (subCategoryValue && categoryExists) {
                const subcategoryExists = window.allItems.some(function (item) {
                    return item.category && item.category.toLowerCase() === categoryValue.toLowerCase() &&
                        item.subCategory && item.subCategory.toLowerCase() === subCategoryValue.toLowerCase();
                });

                if (subcategoryExists) {
                    errorMessages.push(`"${categoryValue}" kategorisinde "${subCategoryValue}" alt kategorisi zaten mevcut.`);
                    if (newSubCategory) newSubCategory.classList.add('is-invalid');
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

        // Get form elements
        const categorySelect = document.getElementById('selectedCategory');
        const subCategorySelect = document.getElementById('selectedSubCategory');
        const extendContentBox = document.getElementById('extendContent');

        // Clear previous validation state
        if (categorySelect) categorySelect.classList.remove('is-invalid');
        if (subCategorySelect) subCategorySelect.classList.remove('is-invalid');
        if (extendContentBox) extendContentBox.classList.remove('is-invalid');

        // Validate Category selection
        if (!categorySelect || !categorySelect.value) {
            errorMessages.push('Lütfen bir kategori seçiniz.');
            if (categorySelect) categorySelect.classList.add('is-invalid');
            hasErrors = true;
        }

        // Validate SubCategory selection
        if (!subCategorySelect || !subCategorySelect.value) {
            errorMessages.push('Lütfen bir alt kategori seçiniz.');
            if (subCategorySelect) subCategorySelect.classList.add('is-invalid');
            hasErrors = true;
        }

        // Validate Content
        if (extendContentBox && !extendContentBox.value.trim()) {
            errorMessages.push('İçerik boş olamaz.');
            extendContentBox.classList.add('is-invalid');
            hasErrors = true;
        }

        if (hasErrors) {
            event.preventDefault(); // Prevent form submission
            notifications.show(errorMessages.join('<br>'), 'warning');
        }
    }

    return {
        initialize,
        validateNewContentForm,
        validateExtendContentForm
    };
})();