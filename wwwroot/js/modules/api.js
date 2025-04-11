// wwwroot/js/modules/api.js
const api = (function() {
    /**
     * API çağrısı yapar ve hata işleme mantığı içerir
     * @param {string} url - API endpoint'i
     * @param {Object} options - Fetch API seçenekleri
     * @param {Function} successCallback - Başarılı yanıt için callback fonksiyonu
     * @param {string} errorContext - Hata durumunda gösterilecek bağlam
     * @returns {Promise<any>} - API yanıtı
     */
    async function call(url, options, successCallback, errorContext) {
        try {
            const response = await fetch(url, options);

            // HTTP hata durumları için kontrol
            if (!response.ok) {
                notifications.show(`${errorContext} sırasında bir hata oluştu: ${response.status} ${response.statusText}`, 'danger');
                return null;
            }

            const data = await response.json();

            // ErrorResponse formatındaki yanıtlar için kontrol
            if (data && typeof data === 'object' && data.hasOwnProperty('success')) {
                if (!data.success) {
                    // API'dan dönen hata mesajını göster
                    if (data.validationErrors && data.validationErrors.length > 0) {
                        notifications.show(data.validationErrors.join('<br>'), 'warning');
                    } else {
                        notifications.show(data.message || `${errorContext} sırasında bir hata oluştu.`, 'danger');
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
            notifications.show(`${errorContext} sırasında bir hata oluştu.`, 'danger');
            return null;
        }
    }

    /**
     * Sunucudan içerik öğelerini yeniden çeker.
     * @param {Function} [callback] - Veriler yüklendikten sonra çağrılacak fonksiyon
     */
    async function getContentItems(callback) {
        const data = await call(
            '/Content/GetContentItems',
            { method: 'GET' },
            function (data) {
                // GetContentItems başarılı yanıtı doğrudan dizi veya success:true ve data:[] formatında olabilir
                if (Array.isArray(data)) {
                    allItems = data;
                    if (callback) callback();
                    return data;
                } else if (data && data.data && Array.isArray(data.data)) {
                    allItems = data.data;
                    if (callback) callback();
                    return data.data;
                } else {
                    console.error("Beklenmeyen veri formatı:", data);
                    notifications.show("İçerik öğeleri beklenmeyen formatta.", "warning");
                    return [];
                }
            },
            'İçerik öğeleri yüklenirken'
        );
        
        return data;
    }

    /**
     * Kategori güncelleme API çağrısı
     * @param {string} oldCategory - Eski kategori adı
     * @param {string} newCategory - Yeni kategori adı
     * @returns {Promise<any>} - API yanıtı
     */
    async function updateCategory(oldCategory, newCategory) {
        const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
        return await call(
            '/Content/UpdateCategory',
            {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': token
                },
                body: JSON.stringify({
                    OldCategory: oldCategory,
                    NewCategory: newCategory
                })
            },
            null,
            'Kategori güncellenirken'
        );
    }

    // Enhanced updateSubCategory function in wwwroot/js/modules/api.js

    /**
     * Alt kategori güncelleme API çağrısı
     * @param {string} category - Kategori adı
     * @param {string} oldSubCategory - Eski alt kategori adı
     * @param {string} newSubCategory - Yeni alt kategori adı
     * @returns {Promise<any>} - API yanıtı
     */
    async function updateSubCategory(category, oldSubCategory, newSubCategory) {
        try {
            const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
            const response = await call(
                '/Content/UpdateSubCategory',
                {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': token
                    },
                    body: JSON.stringify({
                        Category: category,
                        OldSubCategory: oldSubCategory,
                        NewSubCategory: newSubCategory
                    })
                },
                null,
                'Alt kategori güncellenirken'
            );

            // Force a complete refresh of all content items after updating
            if (response && response.success) {
                await getContentItems(); // Updated to not force reload to use correct caching
            }

            return response;
        } catch (error) {
            console.error("Error in updateSubCategory:", error);
            // Return a structured error object 
            return {
                success: false,
                message: `Alt kategori güncellenirken bir hata oluştu: ${error.message}`,
                error: error
            };
        }
    }

    /**
     * Yeni alt kategori ekleme API çağrısı
     * @param {string} category - Kategori adı
     * @param {string} newSubCategory - Yeni alt kategori adı
     * @returns {Promise<any>} - API yanıtı
     */
    async function addSubCategory(category, newSubCategory) {
        const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
        return await call(
            '/Content/AddSubCategory',
            {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': token
                },
                body: JSON.stringify({
                    Category: category,
                    NewSubCategory: newSubCategory
                })
            },
            null,
            'Alt kategori eklenirken'
        );
    }

    /**
     * Kategori silme API çağrısı
     * @param {string} category - Silinecek kategori adı
     * @returns {Promise<any>} - API yanıtı
     */
    async function deleteCategory(category) {
        const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
        return await call(
            '/Content/DeleteCategory',
            {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': token
                },
                body: JSON.stringify({ Category: category })
            },
            null,
            'Kategori silinirken'
        );
    }

    return {
        call,
        getContentItems,
        updateCategory,
        updateSubCategory,
        addSubCategory,
        deleteCategory
    };
})();