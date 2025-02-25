// wwwroot/js/modules/subcategoryManager.js
const subcategoryManager = (function() {
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
        // "Alt kategori seçiniz" opsiyonu hariç mevcut opsiyonları temizler
        while (subCategorySelect.options.length > 1) {
            subCategorySelect.remove(1);
        }
        if (!category) return;

        // Seçilen kategoriye ait alt kategorileri bulur
        var subcats = new Set();
        allItems.forEach(function (item) {
            if (item.category && item.category.trim().toLowerCase() === category.trim().toLowerCase()) {
                subcats.add(item.subCategory ? item.subCategory.trim() : "");
            }
        });

        // Bulunan alt kategorileri sırala ve select'e ekle
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
        
        oldSubCategoryValue = subCategorySelect.value;

        // Kullanıcının yeni seçtiği alt kategoriyi gizli input'a yazar
        hiddenSelectedSubCategory.value = subCategorySelect.value;

        // Eğer alt kategori seçildiyse ilgili alanları görünür yap, aksi halde gizle
        if (subCategorySelect.value) {
            document.getElementById('editSubCategoryBtn').style.display = 'block';
            var matches = allItems.filter(function (item) {
                return item.category.toLowerCase() === document.getElementById('selectedCategory').value.toLowerCase() &&
                    item.subCategory.toLowerCase() === subCategorySelect.value.toLowerCase();
            });
            extendContentBox.value = matches.length > 0 ? matches[0].content : "";
            document.getElementById('contentDiv').style.display = 'block';
            document.getElementById('submitContentBtn').style.display = 'block';
        } else {
            document.getElementById('editSubCategoryBtn').style.display = 'none';
            document.getElementById('contentDiv').style.display = 'none';
            document.getElementById('submitContentBtn').style.display = 'none';
        }
    }
    
    /**
     * Alt kategori düzenleme alanını açıp kapatır
     */
    function toggleEdit() {
        var editDiv = document.getElementById('editSubCategoryDiv');
        if (!editDiv.style.display || editDiv.style.display === 'none') {
            // Alt kategori düzenleme girişine mevcut alt kategori değerini yaz
            document.getElementById('editedSubCategory').value = document.getElementById('selectedSubCategory').value;
            editDiv.style.display = 'block';
        } else {
            editDiv.style.display = 'none';
        }
    }
    
    /**
     * Alt kategori adını kaydeder
     */
    async function saveEdited() {
        const categorySelect = document.getElementById('selectedCategory');
        const subCategorySelect = document.getElementById('selectedSubCategory');
        const hiddenSelectedSubCategory = document.getElementById('hiddenSelectedSubCategory');
        
        var newSubCategory = document.getElementById('editedSubCategory').value.trim();
        if (!newSubCategory) {
            notifications.show("Alt kategori ismi boş olamaz.", "warning");
            return;
        }

        const response = await api.updateSubCategory(
            categorySelect.value, 
            oldSubCategoryValue, 
            newSubCategory
        );
        
        if (response && response.success) {
            // Mevcut seçili alt kategoriyi UI'da günceller
            for (var i = 0; i < subCategorySelect.options.length; i++) {
                if (subCategorySelect.options[i].value === subCategorySelect.value) {
                    subCategorySelect.options[i].text = newSubCategory;
                    subCategorySelect.options[i].value = newSubCategory;
                    break;
                }
            }

            subCategorySelect.value = newSubCategory;
            document.getElementById('editSubCategoryDiv').style.display = 'none';
            hiddenSelectedSubCategory.value = newSubCategory;
            oldSubCategoryValue = newSubCategory;

            api.getContentItems(function () {
                populateSubCategories(categorySelect.value);
            });

            notifications.show(response.message || "Alt kategori başarıyla güncellendi.", "success");
        }
    }
    
    /**
     * Yeni alt kategori ekleme alanını açıp kapatır
     */
    function toggleAdd() {
        var addDiv = document.getElementById('addSubCategoryDiv');
        addDiv.style.display = (!addDiv.style.display || addDiv.style.display === 'none')
            ? 'block' : 'none';
        document.getElementById('editSubCategoryDiv').style.display = 'none';
    }
    
    /**
     * Yeni alt kategori bilgisini kaydeder
     */
    async function saveNew() {
        const categorySelect = document.getElementById('selectedCategory');
        const subCategorySelect = document.getElementById('selectedSubCategory');
        const hiddenSelectedSubCategory = document.getElementById('hiddenSelectedSubCategory');
        
        var newSubCatInput = document.getElementById('newSubCategoryInput');
        var newSubCatValue = newSubCatInput.value.trim();
        if (!newSubCatValue) {
            notifications.show("Yeni alt kategori ismi boş olamaz.", "warning");
            return;
        }

        // Var olan alt kategori listesinde aynı isim varsa eklemeye izin verme
        var exists = false;
        for (var i = 0; i < subCategorySelect.options.length; i++) {
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
            var newOption = document.createElement('option');
            newOption.value = newSubCatValue;
            newOption.textContent = newSubCatValue;
            subCategorySelect.appendChild(newOption);
            subCategorySelect.value = newSubCatValue;

            // Seçili alt kategori olarak güncelle
            hiddenSelectedSubCategory.value = newSubCatValue;

            api.getContentItems(function () {
                populateSubCategories(categorySelect.value);
                onChange();
            });

            document.getElementById('addSubCategoryDiv').style.display = 'none';
            newSubCatInput.value = "";

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