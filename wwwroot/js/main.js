﻿// wwwroot/js/main.js
document.addEventListener('DOMContentLoaded', function () {
    // Global variable for content items
    window.allItems = allItemsJsonData || [];

    // Print debug info about loaded items
    console.log('Total items loaded:', window.allItems.length);
    if (window.allItems.length > 0) {
        console.log('Sample item:', window.allItems[0]);
    }

    // Initialize all modules
    categoryManager.initialize();
    subcategoryManager.initialize();
    formValidation.initialize();

    // Get current department from hidden field
    window.currentDepartment = parseInt(document.getElementById('currentDepartment')?.value || '0', 10);
    console.log('Current department set to:', window.currentDepartment);

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
    initializeExistingData();

    // Display server-side messages if they exist
    displayServerMessages();

    // Set up tab change event handlers
    setupTabChangeHandlers();

    // Initialize department dropdown in navbar
    initializeDepartmentDropdown();
});

// Initialize department dropdown in navbar
function initializeDepartmentDropdown() {
    const departmentDropdown = document.getElementById('departmentDropdown');
    if (departmentDropdown) {
        // Add click handler for department dropdown items
        const departmentItems = document.querySelectorAll('.dropdown-menu button.dropdown-item');
        departmentItems.forEach(item => {
            item.addEventListener('click', function () {
                // The form submission will happen automatically
                item.closest('form').submit();
            });
        });
    }
}

// Initialize with existing data if available
function initializeExistingData() {
    const categorySelect = document.getElementById('selectedCategory');
    if (categorySelect && categorySelect.value) {
        console.log('Initializing with selected category:', categorySelect.value);

        api.getContentItems(function () {
            const subCategoryContainer = document.getElementById('subCategoryContainer');
            if (subCategoryContainer) {
                subCategoryContainer.style.display = 'block';
            }

            subcategoryManager.populateSubCategories(categorySelect.value);

            // Daha önce seçili olan alt kategori varsa onu tekrar seç
            var preselectedSub = document.getElementById('hiddenSelectedSubCategory');
            if (preselectedSub && preselectedSub.value) {
                console.log('Preselected subcategory found:', preselectedSub.value);

                const subCategorySelect = document.getElementById('selectedSubCategory');
                if (subCategorySelect) {
                    subCategorySelect.value = preselectedSub.value;
                    // Trigger the change event to load content
                    subcategoryManager.onChange();
                }
            }
        });
    }
}

// Display server-side messages
function displayServerMessages() {
    if (typeof tempData !== 'undefined') {
        if (tempData.errorMessage) {
            notifications.show(tempData.errorMessage, 'danger');
        }

        if (tempData.successMessage) {
            notifications.show(tempData.successMessage, 'success');
        }
    }
}

// Set up tab change event handlers
function setupTabChangeHandlers() {
    $('a[data-toggle="tab"]').on('shown.bs.tab', function (e) {
        var target = $(e.target).attr("href");
        console.log('Tab changed to:', target);

        // "Var Olan İçeriği Düzenle" sekmesine geçiliyorsa
        if (target === "#extendContentSection") {
            const categorySelect = document.getElementById('selectedCategory');
            if (categorySelect && categorySelect.value) {
                console.log('Loading subcategories for category:', categorySelect.value);

                api.getContentItems(function () {
                    const subCategoryContainer = document.getElementById('subCategoryContainer');
                    if (subCategoryContainer) {
                        subCategoryContainer.style.display = 'block';
                    }

                    subcategoryManager.populateSubCategories(categorySelect.value);

                    // Önceden seçilmiş alt kategori varsa onu seçip onChange çağır
                    var preselectedSub = document.getElementById('hiddenSelectedSubCategory');
                    if (preselectedSub && preselectedSub.value) {
                        console.log('Setting preselected subcategory:', preselectedSub.value);

                        const subCategorySelect = document.getElementById('selectedSubCategory');
                        if (subCategorySelect) {
                            subCategorySelect.value = preselectedSub.value;
                            subcategoryManager.onChange();
                        }
                    }
                });
            }
        }
    });
}