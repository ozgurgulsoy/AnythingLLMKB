// wwwroot/js/main.js
// Add these functions to your existing main.js

// Expose function for deleting content
window.deleteContentItem = function (category, subcategory, department, element) {
    // Confirm deletion
    if (confirm(`"${category}/${subcategory}" içeriğini silmek istediğinize emin misiniz?`)) {
        // Get the anti-forgery token
        const token = document.querySelector('input[name="__RequestVerificationToken"]').value;

        // Create form data
        const formData = new FormData();
        formData.append('category', category);
        formData.append('subcategory', subcategory);
        formData.append('department', department);
        formData.append('__RequestVerificationToken', token);

        // Send AJAX request
        fetch('/Content/DeleteContent', {
            method: 'POST',
            body: formData,
            headers: {
                'X-Requested-With': 'XMLHttpRequest'
            }
        })
            .then(response => response.json())
            .then(data => {
                if (data.success) {
                    // Show success message using existing notifications module
                    notifications.show(data.message || "İçerik başarıyla silindi.", "success");

                    // Remove the card from the UI
                    const card = element.closest('.col-md-4');
                    card.style.opacity = '0';
                    setTimeout(() => {
                        card.remove();
                    }, 300);
                } else {
                    // Show error message
                    notifications.show(data.message || "İçerik silinirken bir hata oluştu.", "danger");
                }
            })
            .catch(error => {
                console.error('Error:', error);
                notifications.show("İçerik silinirken bir hata oluştu.", "danger");
            });
    }
};

// Initialize content deletion - to be called during document ready
function initializeContentDeletion() {
    // Check if Font Awesome is loaded, add if necessary
    if (!document.querySelector('link[href*="font-awesome"]')) {
        const fontAwesome = document.createElement('link');
        fontAwesome.rel = 'stylesheet';
        fontAwesome.href = 'https://cdnjs.cloudflare.com/ajax/libs/font-awesome/5.15.3/css/all.min.css';
        document.head.appendChild(fontAwesome);
    }

    // Add event listeners to delete icons
    document.querySelectorAll('.content-delete-icon').forEach(icon => {
        icon.addEventListener('click', function (e) {
            e.preventDefault();
            e.stopPropagation();

            const category = this.getAttribute('data-category');
            const subcategory = this.getAttribute('data-subcategory');
            const department = this.getAttribute('data-department');

            window.deleteContentItem(category, subcategory, department, this);
        });
    });
}

// Add this to your existing document ready function
document.addEventListener('DOMContentLoaded', function () {
    // Your existing code...

    // Add this line to initialize content deletion
    initializeContentDeletion();
});
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

    // Add this line to trigger content loading for the active tab
    setTimeout(triggerActiveTabContentLoading, 500);

    // Initialize content delete functionality
    initializeContentDelete();
});

// Initialize content delete functionality
function initializeContentDelete() {
    // Add Font Awesome if not already included
    if (!document.querySelector('link[href*="font-awesome"]')) {
        const fontAwesome = document.createElement('link');
        fontAwesome.rel = 'stylesheet';
        fontAwesome.href = 'https://cdnjs.cloudflare.com/ajax/libs/font-awesome/5.15.3/css/all.min.css';
        document.head.appendChild(fontAwesome);
    }

    // Add click handler for delete icons
    document.addEventListener('click', function (e) {
        // Find closest delete icon if clicked on or inside it
        const deleteIcon = e.target.closest('.content-delete-icon');
        if (!deleteIcon) return;

        e.stopPropagation();

        const category = deleteIcon.dataset.category;
        const subcategory = deleteIcon.dataset.subcategory;
        const department = deleteIcon.dataset.department;

        if (confirm(`"${category}/${subcategory}" içeriğini silmek istediğinize emin misiniz?`)) {
            // Get the anti-forgery token
            const token = document.querySelector('input[name="__RequestVerificationToken"]').value;

            // Create form data
            const formData = new FormData();
            formData.append('category', category);
            formData.append('subcategory', subcategory);
            formData.append('department', department);
            formData.append('__RequestVerificationToken', token);

            // Send delete request
            fetch('/Content/DeleteContent', {
                method: 'POST',
                body: formData,
                headers: {
                    'X-Requested-With': 'XMLHttpRequest'
                }
            })
                .then(response => response.json())
                .then(data => {
                    if (data.success) {
                        // Show success message
                        if (typeof notifications !== 'undefined' && notifications.show) {
                            notifications.show(data.message || "İçerik başarıyla silindi.", "success");
                        } else {
                            alert(data.message || "İçerik başarıyla silindi.");
                        }

                        // Remove the card from the UI
                        const card = deleteIcon.closest('.col-md-4');
                        card.style.opacity = '0';
                        setTimeout(() => {
                            card.remove();
                        }, 300);
                    } else {
                        // Show error message
                        if (typeof notifications !== 'undefined' && notifications.show) {
                            notifications.show(data.message || "İçerik silinirken bir hata oluştu.", "danger");
                        } else {
                            alert(data.message || "İçerik silinirken bir hata oluştu.");
                        }
                    }
                })
                .catch(error => {
                    console.error('Error:', error);
                    // Show generic error message
                    if (typeof notifications !== 'undefined' && notifications.show) {
                        notifications.show("İçerik silinirken bir hata oluştu.", "danger");
                    } else {
                        alert("İçerik silinirken bir hata oluştu.");
                    }
                });
        }
    });
}

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

/**
 * Actively loaded tab için içerik yükleme işlemini tetikler
 */
function triggerActiveTabContentLoading() {
    console.log('Triggering active tab content loading...');

    // Check which tab is active
    const activeTabLink = document.querySelector('.nav-link.active');
    if (activeTabLink) {
        const targetId = activeTabLink.getAttribute('href');
        console.log('Active tab target:', targetId);

        if (targetId === "#extendContentSection") {
            // Manually trigger the same logic as in the tab change handler
            const categorySelect = document.getElementById('selectedCategory');
            if (categorySelect && categorySelect.value) {
                console.log('Loading subcategories for category:', categorySelect.value);

                // Show the container first
                const subCategoryContainer = document.getElementById('subCategoryContainer');
                if (subCategoryContainer) {
                    subCategoryContainer.style.display = 'block';
                }

                // Make sure allItems is loaded
                if (window.allItems && Array.isArray(window.allItems)) {
                    subcategoryManager.populateSubCategories(categorySelect.value);

                    // Check for preselected subcategory
                    var preselectedSub = document.getElementById('hiddenSelectedSubCategory');
                    if (preselectedSub && preselectedSub.value) {
                        console.log('Setting preselected subcategory:', preselectedSub.value);

                        const subCategorySelect = document.getElementById('selectedSubCategory');
                        if (subCategorySelect) {
                            subCategorySelect.value = preselectedSub.value;
                            subcategoryManager.onChange();
                        }
                    } else if (document.getElementById('selectedSubCategory') &&
                        document.getElementById('selectedSubCategory').options.length > 1) {
                        // Select first available subcategory
                        document.getElementById('selectedSubCategory').selectedIndex = 1;
                        subcategoryManager.onChange();
                    }
                } else {
                    console.log('Waiting for allItems data...');
                    // Try again in a moment if allItems isn't loaded yet
                    setTimeout(function () {
                        if (window.allItems && Array.isArray(window.allItems)) {
                            console.log('Retrying with allItems:', window.allItems.length);
                            subcategoryManager.populateSubCategories(categorySelect.value);

                            const subCategorySelect = document.getElementById('selectedSubCategory');
                            if (subCategorySelect && subCategorySelect.options.length > 1) {
                                subCategorySelect.selectedIndex = 1;
                                subcategoryManager.onChange();
                            }
                        }
                    }, 1000);
                }
            }
        }
    }
}