document.addEventListener('DOMContentLoaded', function () {
    // Global değişkenler
    var oldCategoryValue = "";
    var oldSubCategoryValue = "";
    var allItems = allItemsJsonData;

    // Sayfada kullanılan öğeler
    var categorySelect = document.getElementById('selectedCategory');
    var subCategorySelect = document.getElementById('selectedSubCategory');
    var extendContentBox = document.getElementById('extendContent');
    var subCategoryContainer = document.getElementById('subCategoryContainer');

    // Kullanıcının seçili alt kategorisini saklamak için gizli input
    var hiddenSelectedSubCategory = document.getElementById('hiddenSelectedSubCategory');

    function setErrorText(elementId, message) {
        document.getElementById(elementId).textContent = message;
    }
    window.setErrorText = setErrorText;

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

    window.onCategoryChange = function () {
        setErrorText('editCategoryError', '');
        oldCategoryValue = categorySelect.value;

        // Yeni kategori seçilince gizli alt kategori değeri temizlenir
        hiddenSelectedSubCategory.value = "";

        subCategoryContainer.style.display = categorySelect.value ? 'block' : 'none';
        refreshAllItems(function () {
            document.getElementById('editCategoryBtn').style.display =
                categorySelect.value ? 'block' : 'none';

            populateSubCategories(categorySelect.value);

            // Alt kategori seçimi sıfırlanır
            subCategorySelect.value = "";
            document.getElementById('editSubCategoryBtn').style.display = 'none';
            document.getElementById('contentDiv').style.display = 'none';
            document.getElementById('submitContentBtn').style.display = 'none';
        });
    };

    window.toggleEditCategory = function () {
        var editDiv = document.getElementById('editCategoryDiv');
        if (!editDiv.style.display || editDiv.style.display === 'none') {
            document.getElementById('editedCategory').value = categorySelect.value;
            editDiv.style.display = 'block';
        } else {
            editDiv.style.display = 'none';
        }
    };

    window.saveEditedCategory = function () {
        setErrorText('editCategoryError', '');
        var newCategory = document.getElementById('editedCategory').value.trim();
        if (!newCategory) {
            setErrorText('editCategoryError', "Kategori ismi boş olamaz.");
            return;
        }
        for (var i = 0; i < categorySelect.options.length; i++) {
            if (categorySelect.options[i].value === categorySelect.value) {
                categorySelect.options[i].text = newCategory;
                categorySelect.options[i].value = newCategory;
                break;
            }
        }
        categorySelect.value = newCategory;
        document.getElementById('editCategoryDiv').style.display = 'none';

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
                    refreshAllItems(function () {
                        populateSubCategories(newCategory);
                    });
                }
            })
            .catch(error => {
                setErrorText('editCategoryError', "Kategori güncellenirken bir hata oluştu.");
            });
    };

    window.populateSubCategories = function (category) {
        while (subCategorySelect.options.length > 1) {
            subCategorySelect.remove(1);
        }
        if (!category) return;

        var subcats = new Set();
        allItems.forEach(function (item) {
            if (item.category && item.category.trim().toLowerCase() === category.trim().toLowerCase()) {
                subcats.add(item.subCategory ? item.subCategory.trim() : "");
            }
        });

        var subcatArr = Array.from(subcats).sort();
        subcatArr.forEach(function (subcat) {
            var opt = document.createElement('option');
            opt.value = subcat;
            opt.textContent = subcat;
            subCategorySelect.appendChild(opt);
        });
    };

    window.onSubCategoryChange = function () {
        setErrorText('editSubCategoryError', '');
        oldSubCategoryValue = subCategorySelect.value;

        // Seçili alt kategori gizli input'a yazılır
        hiddenSelectedSubCategory.value = subCategorySelect.value;

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

    window.toggleEditSubCategory = function () {
        var editDiv = document.getElementById('editSubCategoryDiv');
        if (!editDiv.style.display || editDiv.style.display === 'none') {
            document.getElementById('editedSubCategory').value = subCategorySelect.value;
            editDiv.style.display = 'block';
        } else {
            editDiv.style.display = 'none';
        }
    };

    window.saveEditedSubCategory = function () {
        setErrorText('editSubCategoryError', '');
        var newSubCategory = document.getElementById('editedSubCategory').value.trim();
        if (!newSubCategory) {
            setErrorText('editSubCategoryError', "Alt kategori ismi boş olamaz.");
            return;
        }
        for (var i = 0; i < subCategorySelect.options.length; i++) {
            if (subCategorySelect.options[i].value === subCategorySelect.value) {
                subCategorySelect.options[i].text = newSubCategory;
                subCategorySelect.options[i].value = newSubCategory;
                break;
            }
        }
        subCategorySelect.value = newSubCategory;
        document.getElementById('editSubCategoryDiv').style.display = 'none';

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

    window.toggleAddSubCategory = function () {
        var addDiv = document.getElementById('addSubCategoryDiv');
        addDiv.style.display = (!addDiv.style.display || addDiv.style.display === 'none')
            ? 'block' : 'none';
        document.getElementById('editSubCategoryDiv').style.display = 'none';
    };

    window.saveNewSubCategory = function () {
        var newSubCatInput = document.getElementById('newSubCategoryInput');
        var newSubCatValue = newSubCatInput.value.trim();
        if (!newSubCatValue) {
            setErrorText('editSubCategoryError', "Yeni alt kategori ismi boş olamaz.");
            return;
        }
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
                    var newOption = document.createElement('option');
                    newOption.value = newSubCatValue;
                    newOption.textContent = newSubCatValue;
                    subCategorySelect.appendChild(newOption);
                    subCategorySelect.value = newSubCatValue;
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

    window.confirmDeleteCategory = function () {
        setErrorText('deleteCategoryError', '');
        var category = document.getElementById('deleteCategorySelect').value;
        if (!category) {
            setErrorText('deleteCategoryError', "Lütfen silinecek kategoriyi seçiniz.");
            return;
        }
        document.getElementById('inlineDeleteConfirm').style.display = 'block';
    };

    window.cancelDelete = function () {
        document.getElementById('inlineDeleteConfirm').style.display = 'none';
    };

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

    // Sayfa ilk yüklendiğinde seçili bir kategori varsa alt kategorileri getirir
    if (categorySelect && categorySelect.value) {
        refreshAllItems(function () {
            subCategoryContainer.style.display = 'block';
            populateSubCategories(categorySelect.value);

            var preselectedSub = hiddenSelectedSubCategory.value;
            if (preselectedSub) {
                subCategorySelect.value = preselectedSub;
                onSubCategoryChange();
            }
        });
    }

    // Sekmeler arası geçişte alt kategorileri yeniler
    $('a[data-toggle="tab"]').on('shown.bs.tab', function (e) {
        var target = $(e.target).attr("href");
        if (target === "#extendContentSection") {
            if (categorySelect && categorySelect.value) {
                refreshAllItems(function () {
                    subCategoryContainer.style.display = 'block';
                    populateSubCategories(categorySelect.value);

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
