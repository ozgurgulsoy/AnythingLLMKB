document.addEventListener('DOMContentLoaded', function () {
    // Global değişkenler
    var oldCategoryValue = "";
    var oldSubCategoryValue = "";
    var allItems = allItemsJsonData;

    // DOM öğelerinin referansları
    var categorySelect = document.getElementById('selectedCategory');
    var subCategorySelect = document.getElementById('selectedSubCategory');
    var extendContentBox = document.getElementById('extendContent');
    var subCategoryContainer = document.getElementById('subCategoryContainer');
    var hiddenSelectedSubCategory = document.getElementById('hiddenSelectedSubCategory');

    /**
     * Kullanıcıya bildirim mesajı gösterir
     * @param {string} message - Gösterilecek mesaj
     * @param {string} type - Mesaj tipi (success, error, warning, info)
     * @param {number} duration - Mesajın görüntülenme süresi (ms)
     */
    function showNotification(message, type = 'success', duration = 3000) {
        if (!message) return; // Boş mesajlar için gösterme

        const alertDiv = document.createElement('div');
        alertDiv.className = `alert alert-${type} alert-dismissible fade show`;
        alertDiv.role = "alert";
        alertDiv.innerHTML = message +
            '<button type="button" class="close" data-dismiss="alert" aria-label="Kapat">' +
            '<span aria-hidden="true">&times;</span></button>';

        // Mevcut bildirimleri kaldır
        const existingAlerts = document.querySelectorAll('.alert');
        existingAlerts.forEach(alert => alert.remove());

        // Yeni bildirimi ekle
        const container = document.querySelector('.container');
        if (container) {
            container.insertBefore(alertDiv, container.firstChild);

            if (duration > 0) {
                setTimeout(function () {
                    alertDiv.remove();
                }, duration);
            }
        }
    }
    window.showNotification = showNotification;

    /**
     * API çağrısı yapar ve hata işleme mantığı içerir
     * @param {string} url - API endpoint'i
     * @param {Object} options - Fetch API seçenekleri
     * @param {Function} successCallback - Başarılı yanıt için callback fonksiyonu
     * @param {string} errorContext - Hata durumunda gösterilecek bağlam
     */
    async function apiCall(url, options, successCallback, errorContext) {
        try {
            const response = await fetch(url, options);

            // HTTP hata durumları için kontrol
            if (!response.ok) {
                showNotification(`${errorContext} sırasında bir hata oluştu: ${response.status} ${response.statusText}`, 'danger');
                return null;
            }

            const data = await response.json();

            // ErrorResponse formatındaki yanıtlar için kontrol
            if (data && typeof data === 'object' && data.hasOwnProperty('success')) {
                if (!data.success) {
                    // API'dan dönen hata mesajını göster
                    if (data.validationErrors && data.validationErrors.length > 0) {
                        showNotification(data.validationErrors.join('<br>'), 'warning');
                    } else {
                        showNotification(data.message || `${errorContext} sırasında bir hata oluştu.`, 'danger');
                    }
                    return null;
                }

                // Başarılı yanıt için callback fonksiyonunu çağır
                if (successCallback) {
                    return successCallback(data);
                }

                return data;
            }
            else {
                // Doğrudan veri döndüren API yanıtları için
                if (successCallback) {
                    return successCallback(data);
                }
                return data;
            }
        } catch (error) {
            console.error(`Error during ${errorContext}:`, error);
            showNotification(`${errorContext} sırasında bir hata oluştu.`, 'danger');
            return null;
        }
    }

    /**
     * Sunucudan içerik öğelerini yeniden çeker.
     * @param {Function} [callback] - Veriler yüklendikten sonra çağrılacak fonksiyon
     */
    function refreshAllItems(callback) {
        apiCall(
            '/Content/GetContentItems',
            { method: 'GET' },
            function (data) {
                // GetContentItems başarılı yanıtı doğrudan dizi veya success:true ve data:[] formatında olabilir
                if (Array.isArray(data)) {
                    allItems = data;
                    if (callback) callback();
                } else if (data && data.data && Array.isArray(data.data)) {
                    allItems = data.data;
                    if (callback) callback();
                } else {
                    console.error("Beklenmeyen veri formatı:", data);
                    showNotification("İçerik öğeleri beklenmeyen formatta.", "warning");
                }
            },
            'İçerik öğeleri yüklenirken'
        );
    }
    window.refreshAllItems = refreshAllItems;

    /**
     * Kategori seçildiğinde çağrılır.
     */
    window.onCategoryChange = function () {
        oldCategoryValue = categorySelect.value;
        hiddenSelectedSubCategory.value = "";
        subCategoryContainer.style.display = categorySelect.value ? 'block' : 'none';

        refreshAllItems(function () {
            document.getElementById('editCategoryBtn').style.display =
                categorySelect.value ? 'block' : 'none';

            populateSubCategories(categorySelect.value);
            subCategorySelect.value = "";
            document.getElementById('editSubCategoryBtn').style.display = 'none';
            document.getElementById('contentDiv').style.display = 'none';
            document.getElementById('submitContentBtn').style.display = 'none';
        });
    };

    /**
     * Kategori düzenleme alanını açıp kapatır.
     */
    window.toggleEditCategory = function () {
        var editDiv = document.getElementById('editCategoryDiv');
        if (!editDiv.style.display || editDiv.style.display === 'none') {
            document.getElementById('editedCategory').value = categorySelect.value;
            editDiv.style.display = 'block';
        } else {
            editDiv.style.display = 'none';
        }
    };

    /**
     * Kategoriyi kaydetmek için çağrılır. Kullanıcı yeni kategori adını girdikten sonra "Kaydet" basar.
     */
    window.saveEditedCategory = function () {
        var newCategory = document.getElementById('editedCategory').value.trim();
        if (!newCategory) {
            showNotification("Kategori ismi boş olamaz.", "warning");
            return;
        }

        var token = document.querySelector('input[name="__RequestVerificationToken"]').value;
        apiCall(
            '/Content/UpdateCategory',
            {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': token
                },
                body: JSON.stringify({
                    OldCategory: oldCategoryValue,
                    NewCategory: newCategory
                })
            },
            function (data) {
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
                refreshAllItems(function () {
                    populateSubCategories(newCategory);
                });

                showNotification(data.message || "Kategori başarıyla güncellendi.", "success");
            },
            'Kategori güncellenirken'
        );
    };

    /**
     * Belirli bir kategori için alt kategori seçeneklerini doldurur.
     * @param {string} category - Seçili kategori
     */
    window.populateSubCategories = function (category) {
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
    };

    /**
     * Alt kategori değiştiğinde çağrılır.
     * İçeriği getirir ve sayfada gösterir.
     */
    window.onSubCategoryChange = function () {
        oldSubCategoryValue = subCategorySelect.value;

        // Kullanıcının yeni seçtiği alt kategoriyi gizli input'a yazar
        hiddenSelectedSubCategory.value = subCategorySelect.value;

        // Eğer alt kategori seçildiyse ilgili alanları görünür yap, aksi halde gizle
        if (subCategorySelect.value) {
            document.getElementById('editSubCategoryBtn').style.display = 'block';
            var matches = allItems.filter(function (item) {
                return item.category.toLowerCase() === categorySelect.value.toLowerCase() &&
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
    };

    /**
     * Alt kategori düzenleme alanını açıp kapatır.
     */
    window.toggleEditSubCategory = function () {
        var editDiv = document.getElementById('editSubCategoryDiv');
        if (!editDiv.style.display || editDiv.style.display === 'none') {
            // Alt kategori düzenleme girişine mevcut alt kategori değerini yaz
            document.getElementById('editedSubCategory').value = subCategorySelect.value;
            editDiv.style.display = 'block';
        } else {
            editDiv.style.display = 'none';
        }
    };

    /**
     * Alt kategori adını kaydeder. Kullanıcı "Kaydet"e bastığında çağrılır.
     */
    window.saveEditedSubCategory = function () {
        var newSubCategory = document.getElementById('editedSubCategory').value.trim();
        if (!newSubCategory) {
            showNotification("Alt kategori ismi boş olamaz.", "warning");
            return;
        }

        var token = document.querySelector('input[name="__RequestVerificationToken"]').value;
        apiCall(
            '/Content/UpdateSubCategory',
            {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': token
                },
                body: JSON.stringify({
                    Category: categorySelect.value,
                    OldSubCategory: oldSubCategoryValue,
                    NewSubCategory: newSubCategory
                })
            },
            function (data) {
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

                refreshAllItems(function () {
                    populateSubCategories(categorySelect.value);
                });

                showNotification(data.message || "Alt kategori başarıyla güncellendi.", "success");
            },
            'Alt kategori güncellenirken'
        );
    };

    /**
     * Yeni alt kategori ekleme alanını açıp kapatır.
     */
    window.toggleAddSubCategory = function () {
        var addDiv = document.getElementById('addSubCategoryDiv');
        addDiv.style.display = (!addDiv.style.display || addDiv.style.display === 'none')
            ? 'block' : 'none';
        document.getElementById('editSubCategoryDiv').style.display = 'none';
    };

    /**
     * Yeni alt kategori bilgisini kaydeder. Kullanıcı "Ekle" butonuna bastığında çağrılır.
     */
    window.saveNewSubCategory = function () {
        var newSubCatInput = document.getElementById('newSubCategoryInput');
        var newSubCatValue = newSubCatInput.value.trim();
        if (!newSubCatValue) {
            showNotification("Yeni alt kategori ismi boş olamaz.", "warning");
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
            showNotification("Bu alt kategori zaten mevcut.", "warning");
            return;
        }

        var token = document.querySelector('input[name="__RequestVerificationToken"]').value;
        apiCall(
            '/Content/AddSubCategory',
            {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': token
                },
                body: JSON.stringify({
                    Category: categorySelect.value,
                    NewSubCategory: newSubCatValue
                })
            },
            function (data) {
                // Yeni alt kategoriyi <select>'e ekle
                var newOption = document.createElement('option');
                newOption.value = newSubCatValue;
                newOption.textContent = newSubCatValue;
                subCategorySelect.appendChild(newOption);
                subCategorySelect.value = newSubCatValue;

                // Seçili alt kategori olarak güncelle
                hiddenSelectedSubCategory.value = newSubCatValue;

                refreshAllItems(function () {
                    populateSubCategories(categorySelect.value);
                    onSubCategoryChange();
                });

                document.getElementById('addSubCategoryDiv').style.display = 'none';
                newSubCatInput.value = "";

                showNotification(data.message || "Yeni alt kategori başarıyla eklendi.", "success");
            },
            'Yeni alt kategori eklenirken'
        );
    };

    /**
     * Kategori silme işlemine başlamak için kullanıcıdan onay ister.
     */
    window.confirmDeleteCategory = function () {
        var category = document.getElementById('deleteCategorySelect').value;
        if (!category) {
            showNotification("Lütfen silinecek kategoriyi seçiniz.", "warning");
            return;
        }
        document.getElementById('inlineDeleteConfirm').style.display = 'block';
    };

    /**
     * Kategori silme onay penceresini iptal eder.
     */
    window.cancelDelete = function () {
        document.getElementById('inlineDeleteConfirm').style.display = 'none';
    };

    /**
     * Kategori silme işlemini sunucuya bildirir.
     */
    window.deleteCategory = function () {
        var select = document.getElementById('deleteCategorySelect');
        var category = select.value;
        document.getElementById('inlineDeleteConfirm').style.display = 'none';

        var token = document.querySelector('input[name="__RequestVerificationToken"]').value;
        apiCall(
            '/Content/DeleteCategory',
            {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': token
                },
                body: JSON.stringify({ Category: category })
            },
            function (data) {
                showNotification(data.message || "Kategori başarıyla silindi.", "success");
                setTimeout(function () {
                    location.reload();
                }, 2000);
            },
            'Kategori silinirken'
        );
    };

    // Form validation for new content
    const newContentForm = document.getElementById('newContentForm');
    if (newContentForm) {
        newContentForm.addEventListener('submit', function (event) {
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
                showNotification(errorMessages.join('<br>'), 'warning');
            }
        });
    }

    // Validation for extend content form
    const extendContentForm = document.getElementById('extendContentForm');
    if (extendContentForm) {
        extendContentForm.addEventListener('submit', function (event) {
            let hasErrors = false;
            let errorMessages = [];

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
                showNotification(errorMessages.join('<br>'), 'warning');
            }
        });
    }

    // Sayfa ilk yüklendiğinde, eğer seçili bir kategori varsa alt kategorileri doldur.
    if (categorySelect && categorySelect.value) {
        refreshAllItems(function () {
            subCategoryContainer.style.display = 'block';
            populateSubCategories(categorySelect.value);

            // Daha önce seçili olan alt kategori varsa onu tekrar seç
            var preselectedSub = hiddenSelectedSubCategory.value;
            if (preselectedSub) {
                subCategorySelect.value = preselectedSub;
                onSubCategoryChange();
            }
        });
    }

    // Display server-side messages if they exist
    if (typeof tempData !== 'undefined') {
        if (tempData.errorMessage) {
            showNotification(tempData.errorMessage, 'danger');
        }

        if (tempData.successMessage) {
            showNotification(tempData.successMessage, 'success');
        }
    }

    /**
     * Sekmeler arası geçiş yapıldığında ilgili öğeleri yeniden yükler.
     */
    $('a[data-toggle="tab"]').on('shown.bs.tab', function (e) {
        var target = $(e.target).attr("href");
        // "Var Olan İçeriği Düzenle" sekmesine geçiliyorsa
        if (target === "#extendContentSection") {
            if (categorySelect && categorySelect.value) {
                refreshAllItems(function () {
                    subCategoryContainer.style.display = 'block';
                    populateSubCategories(categorySelect.value);

                    // Önceden seçilmiş alt kategori varsa onu seçip onSubCategoryChange çağır
                    var preselectedSub = hiddenSelectedSubCategory.value;
                    if (preselectedSub) {
                        subCategorySelect.value = preselectedSub;
                        onSubCategoryChange();
                    }
                });
            }
        }
    });
});