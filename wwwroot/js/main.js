// wwwroot/js/main.js

// Expose function for deleting content
window.deleteContentItem = function (category, subcategory, department, element) {
    // Confirm deletion
    if (confirm(`"${category}/${subcategory}" içeriğini temizlemek istediğinize emin misiniz?`)) {
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
                    notifications.show(data.message || "İçerik başarıyla temizlendi.", "success");

                    // IMPORTANT: Just find the p.card-text element directly and clear its text content
                    // Don't manipulate the visibility or remove any parent elements
                    const cardTextElement = element.closest('.card').querySelector('.card-text');
                    if (cardTextElement) {
                        cardTextElement.textContent = '';
                    }
                } else {
                    // Show error message
                    notifications.show(data.message || "İçerik temizlenirken bir hata oluştu.", "danger");
                }
            })
            .catch(error => {
                console.error('Error:', error);
                notifications.show("İçerik temizlenirken bir hata oluştu.", "danger");
            });
    }
};

// Direct content loading function
function directLoadContent() {
    console.log("Direct content loader triggered");

    // Get current values
    const categorySelect = document.getElementById('selectedCategory');
    const subCategorySelect = document.getElementById('selectedSubCategory');
    const departmentSelect = document.getElementById('extendDepartment');

    if (!categorySelect || !subCategorySelect ||
        !categorySelect.value || !subCategorySelect.value) {
        console.log("Missing category or subcategory selections");
        return;
    }

    const category = categorySelect.value.trim();
    const subcategory = subCategorySelect.value.trim();
    const department = departmentSelect ? parseInt(departmentSelect.value, 10) : (window.currentDepartment || 0);

    console.log(`Directly loading content for ${category}/${subcategory} (Department: ${department})`);
    console.log("Department type:", typeof department);

    // Loop through allItems to find a match
    if (window.allItems && Array.isArray(window.allItems)) {
        console.log(`Searching through ${window.allItems.length} items`);

        // Log first few items to debug
        if (window.allItems.length > 0) {
            console.log("Sample items:", window.allItems.slice(0, 3).map(item => ({
                cat: item.Category || item.category,
                subcat: item.SubCategory || item.subCategory,
                dept: item.Department !== undefined ? item.Department : (item.department !== undefined ? item.department : 0)
            })));
        }

        // Try to find the matching item with better logging
        let found = false;
        for (let i = 0; i < window.allItems.length; i++) {
            const item = window.allItems[i];

            // Get item properties - handle both camelCase and PascalCase
            const itemCategory = (item.Category || item.category || "").trim();
            const itemSubCategory = (item.SubCategory || item.subCategory || "").trim();
            const itemDepartment = item.Department !== undefined ? item.Department :
                (item.department !== undefined ? item.department : 0);

            // Case-insensitive comparison for strings
            const categoryMatch = itemCategory.toLowerCase() === category.toLowerCase();
            const subcategoryMatch = itemSubCategory.toLowerCase() === subcategory.toLowerCase();

            // More flexible department matching
            // Match if either we're not filtering by department (0) or the departments match
            const departmentMatch = department === 0 || itemDepartment === 0 ||
                itemDepartment === department;

            // Debug potential matches
            if (categoryMatch && subcategoryMatch) {
                console.log(`Found potential match: ${itemCategory}/${itemSubCategory} (Dept: ${itemDepartment})`);
                console.log(`Department match: ${departmentMatch} (user: ${department}, item: ${itemDepartment})`);
            }

            if (categoryMatch && subcategoryMatch && departmentMatch) {
                found = true;
                console.log("Match found!", item);

                // Get the content field
                const contentField = document.getElementById('extendContent');
                if (contentField) {
                    // Set the content value
                    const content = item.Content || item.content || "";
                    contentField.value = content;
                    console.log(`Set content field with ${content.length} characters`);

                    // Show the content area
                    const contentDiv = document.getElementById('contentDiv');
                    const submitBtn = document.getElementById('submitContentBtn');

                    if (contentDiv) contentDiv.style.display = 'block';
                    if (submitBtn) submitBtn.style.display = 'block';
                } else {
                    console.error("Content field not found");
                }
                break;
            }
        }

        if (!found) {
            console.log(`No item found for ${category}/${subcategory} with department ${department}`);

            // Show the content area even if no match is found - let user create content
            const contentField = document.getElementById('extendContent');
            if (contentField) {
                contentField.value = ""; // Clear previous content

                // Show the content area for new content creation
                const contentDiv = document.getElementById('contentDiv');
                const submitBtn = document.getElementById('submitContentBtn');

                if (contentDiv) contentDiv.style.display = 'block';
                if (submitBtn) submitBtn.style.display = 'block';
            }
        }
    } else {
        console.error("allItems is not available");
    }
}

document.addEventListener('DOMContentLoaded', function () {
    // Global variable for content items
    window.allItems = (typeof allItemsJsonData !== 'undefined') ? allItemsJsonData : [];

    // Print debug info about loaded items
    console.log('Total items loaded:', window.allItems.length);
    if (window.allItems.length > 0) {
        console.log('Sample item:', window.allItems[0]);
    }

    // Add a special debugging call for category/subcategory pairs
    console.log("All Items by Category/SubCategory:");
    if (window.allItems && window.allItems.length) {
        // Group items by category and subcategory for easier debugging
        const itemMap = {};
        window.allItems.forEach(item => {
            const cat = item.Category || item.category;
            const subcat = item.SubCategory || item.subCategory;
            const dept = item.Department !== undefined ? item.Department :
                (item.department !== undefined ? item.department : 0);

            if (!itemMap[cat]) itemMap[cat] = {};
            if (!itemMap[cat][subcat]) itemMap[cat][subcat] = [];

            itemMap[cat][subcat].push(dept);
        });
        console.log(itemMap);
    }

    // Initialize all modules
    categoryManager.initialize();
    subcategoryManager.initialize();
    formValidation.initialize();

    // Get current department from hidden field
    window.currentDepartment = parseInt(document.getElementById('currentDepartment')?.value || '0', 10);
    console.log('Current department set to:', window.currentDepartment);

    // Event delegation approach - safer for minification and bundling
    document.addEventListener('click', function (e) {
        // Find the closest element with a data-action attribute
        const actionElement = e.target.closest('[data-action]');
        if (!actionElement) return;

        const action = actionElement.dataset.action;

        // Map actions to functions
        const actionMap = {
            'confirm-delete-category': categoryManager.confirmDelete,
            'cancel-delete': categoryManager.cancelDelete,
            'delete-category': categoryManager.deleteSelected,
            'toggle-edit-category': categoryManager.toggleEdit,
            'save-edited-category': categoryManager.saveEdited,
            'toggle-edit-subcategory': subcategoryManager.toggleEdit,
            'save-edited-subcategory': subcategoryManager.saveEdited,
            'toggle-add-subcategory': subcategoryManager.toggleAdd,
            'save-new-subcategory': subcategoryManager.saveNew,
            'delete-content': function () {
                const category = actionElement.getAttribute('data-category');
                const subcategory = actionElement.getAttribute('data-subcategory');
                const department = actionElement.getAttribute('data-department');
                window.deleteContentItem(category, subcategory, department, actionElement);
            }
        };

        if (actionMap[action]) {
            e.preventDefault();
            actionMap[action]();
        }
    });

    // Handle change events separately
    document.addEventListener('change', function (e) {
        const actionElement = e.target.closest('[data-action]');
        if (!actionElement) return;

        const action = actionElement.dataset.action;

        if (action === 'category-change') {
            categoryManager.onChange();
        }
        else if (action === 'subcategory-change') {
            // First call the original handler
            subcategoryManager.onChange();

            // Then trigger direct content loading to ensure content is displayed
            directLoadContent();
        }
    });

    // Expose public methods to window for HTML event handlers
    window.showNotification = notifications.show;
    window.refreshAllItems = api.getContentItems;

    // Initialize content delete functionality
    initializeContentDelete();

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

    // Special handler for the subcategory select element to ensure content loading works
    const subCategorySelect = document.getElementById('selectedSubCategory');
    if (subCategorySelect) {
        subCategorySelect.addEventListener('change', directLoadContent);
    }
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
                // Also directly load content to ensure it's displayed
                directLoadContent();
            }
        }
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
                        // Also directly load content
                        directLoadContent();
                    }
                }
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
                            // Also directly load content
                            directLoadContent();
                        }
                    } else if (document.getElementById('selectedSubCategory') &&
                        document.getElementById('selectedSubCategory').options.length > 1) {
                        // Select first available subcategory
                        document.getElementById('selectedSubCategory').selectedIndex = 1;
                        subcategoryManager.onChange();
                        // Also directly load content
                        directLoadContent();
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
                                // Also directly load content
                                directLoadContent();
                            }
                        }
                    }, 1000);
                }
            }
        }
    }
}