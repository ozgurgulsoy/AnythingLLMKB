// wwwroot/js/modules/subcategoryManager.js
const subcategoryManager = (function () {
    let oldSubCategoryValue = "";

    /**
     * Başlangıç değerlerini ayarlar
     */
    function initialize() {
        const subCategorySelect = document.getElementById('selectedSubCategory');
        if (subCategorySelect) {
            oldSubCategoryValue = subCategorySelect.value;
        }
    }

    /**
     * Belirli bir kategori için alt kategori seçeneklerini doldurur
     * @param {string} category - Seçili kategori
     */
    function populateSubCategories(category) {
        const subCategorySelect = document.getElementById('selectedSubCategory');

        if (!subCategorySelect) {
            console.error('Alt kategori seçim elementi bulunamadı');
            return;
        }

        // "Alt kategori seçiniz" opsiyonu hariç mevcut opsiyonları temizler
        while (subCategorySelect.options.length > 1) {
            subCategorySelect.remove(1);
        }

        if (!category) return;

        // Remove department filtering - get all subcategories for the given category
        console.log('Looking for subcategories for category:', category);

        // Seçilen kategoriye ait alt kategorileri bulur
        const subcats = new Set();

        if (window.allItems && Array.isArray(window.allItems)) {
            console.log('Total items in allItems:', window.allItems.length);

            window.allItems.forEach(function (item) {
                // Try both PascalCase and camelCase to be safe
                const category_value = item.Category || item.category;
                const subcategory_value = item.SubCategory || item.subCategory;

                // Match only by category - removed department filtering
                if (category_value &&
                    category_value.trim().toLowerCase() === category.trim().toLowerCase()) {
                    subcats.add(subcategory_value ? subcategory_value.trim() : "");
                }
            });
        }

        // Bulunan alt kategorileri sırala ve select'e ekle
        console.log('Found subcategories:', Array.from(subcats));
        var subcatArr = Array.from(subcats).sort();
        subcatArr.forEach(function (subcat) {
            var opt = document.createElement('option');
            opt.value = subcat;
            opt.textContent = subcat;
            subCategorySelect.appendChild(opt);
        });
    }

    /**
     * Alt kategori değiştiğinde çağrılır
     */
    function onChange() {
        const subCategorySelect = document.getElementById('selectedSubCategory');
        const hiddenSelectedSubCategory = document.getElementById('hiddenSelectedSubCategory');
        const extendContentBox = document.getElementById('extendContent');
        const editSubCategoryBtn = document.getElementById('editSubCategoryBtn');
        const contentDiv = document.getElementById('contentDiv');
        const submitContentBtn = document.getElementById('submitContentBtn');
        const categorySelect = document.getElementById('selectedCategory');

        if (!subCategorySelect || !categorySelect) {
            console.error('Alt kategori elementi bulunamadı');
            return;
        }

        oldSubCategoryValue = subCategorySelect.value;

        // Kullanıcının yeni seçtiği alt kategoriyi gizli input'a yazar
        if (hiddenSelectedSubCategory) {
            hiddenSelectedSubCategory.value = subCategorySelect.value;
        }

        // Eğer alt kategori seçildiyse ilgili alanları görünür yap, aksi halde gizle
        if (subCategorySelect.value) {
            if (editSubCategoryBtn) {
                editSubCategoryBtn.style.display = 'block';
            }

            let content = "";

            console.log('Looking for match with:', {
                category: categorySelect.value.toLowerCase(),
                subcategory: subCategorySelect.value.toLowerCase()
                // Removed department filter
            });

            if (window.allItems && Array.isArray(window.allItems)) {
                console.log('Searching through', window.allItems.length, 'items');

                // Fixed: Add null/undefined checks for all properties
                const matches = window.allItems.filter(function (item) {
                    const itemCategory = (item.Category || item.category || "").toLowerCase();
                    const itemSubCategory = (item.SubCategory || item.subCategory || "").toLowerCase();

                    // Removed department condition - just match by category and subcategory
                    return itemCategory === categorySelect.value.toLowerCase() &&
                        itemSubCategory === subCategorySelect.value.toLowerCase();
                });

                console.log('Matches found:', matches.length);

                if (matches.length > 0) {
                    content = matches[0].Content || matches[0].content || "";
                }
            }

            if (extendContentBox) {
                extendContentBox.value = content;
            }

            if (contentDiv) {
                contentDiv.style.display = 'block';
            }

            if (submitContentBtn) {
                submitContentBtn.style.display = 'block';
            }
        } else {
            if (editSubCategoryBtn) {
                editSubCategoryBtn.style.display = 'none';
            }

            if (contentDiv) {
                contentDiv.style.display = 'none';
            }

            if (submitContentBtn) {
                submitContentBtn.style.display = 'none';
            }
        }
    }

    /**
     * Alt kategori düzenleme alanını açıp kapatır
     */
    function toggleEdit() {
        const editSubCategoryDiv = document.getElementById('editSubCategoryDiv');
        const subCategorySelect = document.getElementById('selectedSubCategory');
        const editedSubCategory = document.getElementById('editedSubCategory');
        const addSubCategoryDiv = document.getElementById('addSubCategoryDiv');

        if (!editSubCategoryDiv || !subCategorySelect) {
            console.error('Alt kategori düzenleme elementleri bulunamadı');
            return;
        }

        if (!editSubCategoryDiv.style.display || editSubCategoryDiv.style.display === 'none') {
            // Alt kategori düzenleme girişine mevcut alt kategori değerini yaz
            if (editedSubCategory) {
                editedSubCategory.value = subCategorySelect.value;
            }
            editSubCategoryDiv.style.display = 'block';

            // Diğer düzenleme panelini kapat
            if (addSubCategoryDiv) {
                addSubCategoryDiv.style.display = 'none';
            }
        } else {
            editSubCategoryDiv.style.display = 'none';
        }
    }

    /**
     * Alt kategori adını kaydeder
     */
    async function saveEdited() {
        const categorySelect = document.getElementById('selectedCategory');
        const subCategorySelect = document.getElementById('selectedSubCategory');
        const hiddenSelectedSubCategory = document.getElementById('hiddenSelectedSubCategory');
        const editedSubCategory = document.getElementById('editedSubCategory');
        const editSubCategoryDiv = document.getElementById('editSubCategoryDiv');

        if (!categorySelect || !subCategorySelect || !editedSubCategory) {
            console.error('Alt kategori düzenleme elementleri bulunamadı');
            return;
        }

        const newSubCategory = editedSubCategory.value.trim();
        if (!newSubCategory) {
            notifications.show("Alt kategori ismi boş olamaz.", "warning");
            return;
        }

        // Aynı isim kontrolü
        if (newSubCategory === oldSubCategoryValue) {
            notifications.show("Alt kategori ismi değiştirilmedi.", "info");
            if (editSubCategoryDiv) {
                editSubCategoryDiv.style.display = 'none';
            }
            return;
        }

        // İsim uzunluğu kontrolü - simple validation
        if (newSubCategory.length < 2) {
            notifications.show("Alt kategori ismi en az 2 karakter olmalıdır.", "warning");
            return;
        }

        try {
            const response = await api.updateSubCategory(
                categorySelect.value,
                oldSubCategoryValue,
                newSubCategory
            );

            if (response && response.success) {
                // Update was successful
                handleSuccessfulSubcategoryUpdate(categorySelect, subCategorySelect, hiddenSelectedSubCategory,
                    editSubCategoryDiv, newSubCategory, response);
            } else {
                // API call succeeded but returned an error
                handleSubcategoryUpdateError(response);
            }
        } catch (error) {
            // API call failed completely
            console.error("Subcategory update error:", error);
            notifications.show("Alt kategori güncellenirken bir hata oluştu. Lütfen tekrar deneyin.", "danger");
            // Keep the edit panel open when error occurs
        }
    }

    /**
     * Alt kategori başarıyla güncellendiğinde yapılacak işlemler
     */
    function handleSuccessfulSubcategoryUpdate(categorySelect, subCategorySelect, hiddenSelectedSubCategory,
        editSubCategoryDiv, newSubCategory, response) {
        // Önce allItems içindeki tüm ilgili alt kategorileri güncelle
        if (window.allItems && Array.isArray(window.allItems)) {
            window.allItems.forEach(item => {
                // Try both PascalCase and camelCase
                const itemCategory = item.Category || item.category;
                const itemSubCategory = item.SubCategory || item.subCategory;

                if (itemCategory &&
                    itemCategory.trim().toLowerCase() === categorySelect.value.toLowerCase() &&
                    itemSubCategory &&
                    itemSubCategory.trim().toLowerCase() === oldSubCategoryValue.toLowerCase()) {

                    // Update subcategory in the cached items
                    if (item.SubCategory) {
                        item.SubCategory = newSubCategory;
                    } else if (item.subCategory) {
                        item.subCategory = newSubCategory;
                    }
                }
            });
        }

        // Dropdown'daki tüm alt kategorileri temizle ve yeniden doldur
        populateSubCategories(categorySelect.value);

        // Şimdi yeni alt kategoriyi seç
        for (let i = 0; i < subCategorySelect.options.length; i++) {
            if (subCategorySelect.options[i].value.toLowerCase() === newSubCategory.toLowerCase()) {
                subCategorySelect.selectedIndex = i;
                break;
            }
        }

        // Gizli input'u güncelle
        if (hiddenSelectedSubCategory) {
            hiddenSelectedSubCategory.value = newSubCategory;
        }

        // Düzenleme panelini kapat
        if (editSubCategoryDiv) {
            editSubCategoryDiv.style.display = 'none';
        }

        // oldSubCategoryValue değişkenini güncelle
        oldSubCategoryValue = newSubCategory;

        // İçerik alanını güncelle
        onChange();

        notifications.show(response.message || "Alt kategori başarıyla güncellendi.", "success");
    }

    /**
     * Alt kategori güncellenirken hata oluştuğunda yapılacak işlemler
     */
    function handleSubcategoryUpdateError(response) {
        // Hata mesajını göster
        notifications.show(
            response?.message || "Alt kategori güncellenirken bir hata oluştu.",
            "danger"
        );

        // Don't close the edit panel - allow user to fix and retry
        console.log("Subcategory update returned error:", response);
    }

    /**
     * Yeni alt kategori ekleme alanını açıp kapatır
     */
    function toggleAdd() {
        const addSubCategoryDiv = document.getElementById('addSubCategoryDiv');
        const editSubCategoryDiv = document.getElementById('editSubCategoryDiv');
        const newSubCategoryInput = document.getElementById('newSubCategoryInput');

        if (!addSubCategoryDiv) {
            console.error('Alt kategori ekleme elementi bulunamadı');
            return;
        }

        if (!addSubCategoryDiv.style.display || addSubCategoryDiv.style.display === 'none') {
            addSubCategoryDiv.style.display = 'block';

            // Diğer düzenleme panelini kapat
            if (editSubCategoryDiv) {
                editSubCategoryDiv.style.display = 'none';
            }

            // Input'a odaklan
            if (newSubCategoryInput) {
                setTimeout(function () {
                    newSubCategoryInput.focus();
                }, 100);
            }
        } else {
            addSubCategoryDiv.style.display = 'none';
        }
    }

    /**
     * Yeni alt kategori bilgisini kaydeder
     */
    async function saveNew() {
        const categorySelect = document.getElementById('selectedCategory');
        const subCategorySelect = document.getElementById('selectedSubCategory');
        const hiddenSelectedSubCategory = document.getElementById('hiddenSelectedSubCategory');
        const newSubCategoryInput = document.getElementById('newSubCategoryInput');
        const addSubCategoryDiv = document.getElementById('addSubCategoryDiv');

        if (!categorySelect || !subCategorySelect || !newSubCategoryInput) {
            console.error('Alt kategori ekleme elementleri bulunamadı');
            return;
        }

        const newSubCatValue = newSubCategoryInput.value.trim();
        if (!newSubCatValue) {
            notifications.show("Yeni alt kategori ismi boş olamaz.", "warning");
            return;
        }

        // İsim uzunluğu kontrolü - simple validation
        if (newSubCatValue.length < 2) {
            notifications.show("Alt kategori ismi en az 2 karakter olmalıdır.", "warning");
            return;
        }

        // Var olan alt kategori listesinde aynı isim varsa eklemeye izin verme
        let exists = false;
        for (let i = 0; i < subCategorySelect.options.length; i++) {
            if (subCategorySelect.options[i].value.trim().toLowerCase() === newSubCatValue.toLowerCase()) {
                exists = true;
                break;
            }
        }

        if (exists) {
            notifications.show("Bu alt kategori zaten mevcut.", "warning");
            return;
        }

        const response = await api.addSubCategory(categorySelect.value, newSubCatValue);

        if (response && response.success) {
            // Yeni alt kategoriyi <select>'e ekle
            const newOption = document.createElement('option');
            newOption.value = newSubCatValue;
            newOption.textContent = newSubCatValue;
            subCategorySelect.appendChild(newOption);
            subCategorySelect.value = newSubCatValue;

            // Seçili alt kategori olarak güncelle
            if (hiddenSelectedSubCategory) {
                hiddenSelectedSubCategory.value = newSubCatValue;
            }

            api.getContentItems(function () {
                populateSubCategories(categorySelect.value);
                onChange();
            });

            if (addSubCategoryDiv) {
                addSubCategoryDiv.style.display = 'none';
            }

            newSubCategoryInput.value = "";

            notifications.show(response.message || "Yeni alt kategori başarıyla eklendi.", "success");
        }
    }

    return {
        initialize,
        populateSubCategories,
        onChange,
        toggleEdit,
        saveEdited,
        toggleAdd,
        saveNew,
        get oldValue() { return oldSubCategoryValue; }
    };
})();