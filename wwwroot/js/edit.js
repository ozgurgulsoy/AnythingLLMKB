document.addEventListener('DOMContentLoaded', function () {
    // Global değişkenler
    // Bu değişkenler, seçilen kategori/alt kategori ve JSON'dan çekilen içerikleri saklar.
    var oldCategoryValue = "";
    var oldSubCategoryValue = "";
    var allItems = allItemsJsonData;

    // DOM öğelerinin referansları
    var categorySelect = document.getElementById('selectedCategory');
    var subCategorySelect = document.getElementById('selectedSubCategory');
    var extendContentBox = document.getElementById('extendContent');
    var subCategoryContainer = document.getElementById('subCategoryContainer');

    // Kullanıcının aktif olarak seçtiği alt kategoriyi tutan gizli input
    var hiddenSelectedSubCategory = document.getElementById('hiddenSelectedSubCategory');

    /**
     * Belirli bir öğenin içine hata mesajı koyar.
     * @param {string} elementId - Hata mesajının yazılacağı elementin ID'si
     * @param {string} message - Gösterilecek hata metni
     */
    function setErrorText(elementId, message) {
        document.getElementById(elementId).textContent = message;
    }
    window.setErrorText = setErrorText;

    /**
     * Sunucudan içerik öğelerini yeniden çeker.
     * @param {Function} [callback] - Veriler yüklendikten sonra çağrılacak fonksiyon
     */
    function refreshAllItems(callback) {
        fetch('/Home/GetContentItems')
            .then(res => res.json())
            .then(updatedItems => {
                allItems = updatedItems;
                if (callback) callback();
            })
            .catch(error => console.error("Öğeler yenilenirken hata oluştu:", error));
    }
    window.refreshAllItems = refreshAllItems;

    /**
     * Kategori seçildiğinde çağrılır.
     * Kategoriye göre alt kategori alanını ve diğer ilgili alanları ayarlar.
     */
    window.onCategoryChange = function () {
        setErrorText('editCategoryError', '');
        oldCategoryValue = categorySelect.value;

        // Farklı bir kategori seçilince alt kategori seçim bilgisini sıfırla.
        hiddenSelectedSubCategory.value = "";

        // Alt kategori alanını açar/kapatır
        subCategoryContainer.style.display = categorySelect.value ? 'block' : 'none';

        // Sunucudan tekrar içerik yükleyip alt kategorileri yeniler
        refreshAllItems(function () {
            // "Kategoriyi Düzenle" butonunu, sadece geçerli bir kategori varsa göster
            document.getElementById('editCategoryBtn').style.display =
                categorySelect.value ? 'block' : 'none';

            populateSubCategories(categorySelect.value);

            // Alt kategori seçimini temizler
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
        // Kategori düzenleme alanı gizliyse açar, açıksa kapatır
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
        setErrorText('editCategoryError', '');
        var newCategory = document.getElementById('editedCategory').value.trim();
        if (!newCategory) {
            setErrorText('editCategoryError', "Kategori ismi boş olamaz.");
            return;
        }
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

        // Sunucuya kategori değişikliği bilgisini gönderir
        var token = document.querySelector('input[name="__RequestVerificationToken"]').value;
        fetch('/Home/UpdateCategory', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': token
            },
            body: JSON.stringify({ OldCategory: oldCategoryValue, NewCategory: newCategory })
        })
            .then(response => response.json())
            .then(data => {
                if (!data.success) {
                    setErrorText('editCategoryError', data.message || "Kategori güncelleme hatası.");
                } else {
                    oldCategoryValue = newCategory;
                    // Kategoriler güncellendikten sonra tekrar alt kategorileri çek
                    refreshAllItems(function () {
                        populateSubCategories(newCategory);
                    });
                }
            })
            .catch(error => {
                setErrorText('editCategoryError', "Kategori güncellenirken bir hata oluştu.");
            });
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
        setErrorText('editSubCategoryError', '');
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
        setErrorText('editSubCategoryError', '');
        var newSubCategory = document.getElementById('editedSubCategory').value.trim();
        if (!newSubCategory) {
            setErrorText('editSubCategoryError', "Alt kategori ismi boş olamaz.");
            return;
        }
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

        // Sunucuya alt kategori güncelleme isteği gönder
        var token = document.querySelector('input[name="__RequestVerificationToken"]').value;
        fetch('/Home/UpdateSubCategory', {
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
        })
            .then(response => response.json())
            .then(data => {
                if (!data.success) {
                    setErrorText('editSubCategoryError', data.message || "Alt kategori güncelleme hatası.");
                } else {
                    hiddenSelectedSubCategory.value = newSubCategory;
                    oldSubCategoryValue = newSubCategory;
                    refreshAllItems(function () {
                        populateSubCategories(categorySelect.value);
                    });
                }
            })
            .catch(error => {
                setErrorText('editSubCategoryError', "Alt kategori güncellenirken bir hata oluştu.");
            });
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
            setErrorText('editSubCategoryError', "Yeni alt kategori ismi boş olamaz.");
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
            setErrorText('editSubCategoryError', "Bu alt kategori zaten mevcut.");
            return;
        }
        // Sunucuya yeni alt kategori ekleme isteği gönder
        var token = document.querySelector('input[name="__RequestVerificationToken"]').value;
        fetch('/Home/AddSubCategory', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': token
            },
            body: JSON.stringify({
                Category: categorySelect.value,
                NewSubCategory: newSubCatValue
            })
        })
            .then(response => response.json())
            .then(data => {
                if (!data.success) {
                    setErrorText('editSubCategoryError', data.message || "Yeni alt kategori eklenirken hata oluştu.");
                } else {
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
                }
            })
            .catch(error => {
                setErrorText('editSubCategoryError', "Yeni alt kategori eklenirken bir hata oluştu.");
            });
    };

    /**
     * Kategori silme işlemine başlamak için kullanıcıdan onay ister.
     */
    window.confirmDeleteCategory = function () {
        setErrorText('deleteCategoryError', '');
        var category = document.getElementById('deleteCategorySelect').value;
        if (!category) {
            setErrorText('deleteCategoryError', "Lütfen silinecek kategoriyi seçiniz.");
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
        setErrorText('deleteCategoryError', '');

        var token = document.querySelector('input[name="__RequestVerificationToken"]').value;
        fetch('/Home/DeleteCategory', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': token
            },
            body: JSON.stringify({ Category: category })
        })
            .then(response => response.json())
            .then(data => {
                if (!data.success) {
                    setErrorText('deleteCategoryError', data.message || "Kategori silinirken hata oluştu.");
                } else {
                    // Başarılı silme sonrası kullanıcıya mesaj gösterip sayfayı yenile
                    var alertDiv = document.createElement('div');
                    alertDiv.className = "alert alert-success alert-dismissible fade show";
                    alertDiv.role = "alert";
                    alertDiv.innerHTML = "Kategori silindi." +
                        '<button type="button" class="close" data-dismiss="alert" aria-label="Kapat">' +
                        '<span aria-hidden="true">&times;</span></button>';
                    document.querySelector('.container').insertBefore(alertDiv, document.querySelector('.container').firstChild);
                    setTimeout(function () {
                        location.reload();
                    }, 2000);
                }
            })
            .catch(error => {
                setErrorText('deleteCategoryError', "Kategori silinirken bir hata oluştu.");
            });
    };

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
