// wwwroot/js/modules/api.js
const api = (function () {
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
            // Add cache busting parameter to avoid stale data
            const cacheBuster = `_=${Date.now()}`;
            const urlWithCache = url.includes('?') ? `${url}&${cacheBuster}` : `${url}?${cacheBuster}`;

            const response = await fetch(urlWithCache, options);

            // HTTP hata durumları için kontrol
            if (!response.ok) {
                notifications.show(`${errorContext} sırasında bir hata oluştu: ${response.status} ${response.statusText}`, 'danger');
                console.error(`Error during ${errorContext}: ${response.status} ${response.statusText}`);
                return null;
            }

            const data = await response.json();

            // ErrorResponse formatındaki yanıtlar için kontrol
            if (data && typeof data === 'object' && 'success' in data) {
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
            notifications.show(`${errorContext} sırasında bir hata oluştu: ${error.message}`, 'danger');
            return null;
        }
    }

    /**
     * Sunucudan içerik öğelerini yeniden çeker.
     * @param {Function} [callback] - Veriler yüklendikten sonra çağrılacak fonksiyon
     * @param {number} [department] - Departman filtresi için opsiyonel departman ID
     * @returns {Promise<Array>} İçerik öğeleri dizisi
     */
    async function getContentItems(callback, department) {
        const dept = department || window.currentDepartment || 0;
        const url = dept ? `/Content/GetContentItems?department=${dept}` : '/Content/GetContentItems';

        const data = await call(
            url,
            {
                method: 'GET',
                headers: {
                    'Cache-Control': 'no-cache, no-store, must-revalidate',
                    'Pragma': 'no-cache',
                    'Expires': '0'
                }
            },
            function (data) {
                // GetContentItems başarılı yanıtı doğrudan dizi veya success:true ve data:[] formatında olabilir
                if (Array.isArray(data)) {
                    window.allItems = data;
                    if (callback) callback();
                    return data;
                } else if (data && data.data && Array.isArray(data.data)) {
                    window.allItems = data.data;
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

        return data || [];
    }

    /**
     * CSRF token'ını sayfadan alır
     * @returns {string} CSRF token değeri
     */
    function getAntiForgeryToken() {
        const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
        if (!tokenInput) {
            console.error('CSRF token bulunamadı');
            return '';
        }
        return tokenInput.value;
    }

    /**
     * Kategori güncelleme API çağrısı
     * @param {string} oldCategory - Eski kategori adı
     * @param {string} newCategory - Yeni kategori adı
     * @returns {Promise<any>} - API yanıtı
     */
    async function updateCategory(oldCategory, newCategory) {
        const token = getAntiForgeryToken();
        if (!token) {
            notifications.show('Güvenlik token\'ı bulunamadı. Sayfayı yenileyin ve tekrar deneyin.', 'danger');
            return null;
        }

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

    /**
     * Alt kategori güncelleme API çağrısı
     * @param {string} category - Kategori adı
     * @param {string} oldSubCategory - Eski alt kategori adı
     * @param {string} newSubCategory - Yeni alt kategori adı
     * @returns {Promise<any>} - API yanıtı
     */
    async function updateSubCategory(category, oldSubCategory, newSubCategory) {
        const token = getAntiForgeryToken();
        if (!token) {
            notifications.show('Güvenlik token\'ı bulunamadı. Sayfayı yenileyin ve tekrar deneyin.', 'danger');
            return null;
        }

        return await call(
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
    }

    /**
     * Yeni alt kategori ekleme API çağrısı
     * @param {string} category - Kategori adı
     * @param {string} newSubCategory - Yeni alt kategori adı
     * @returns {Promise<any>} - API yanıtı
     */
    async function addSubCategory(category, newSubCategory) {
        const token = getAntiForgeryToken();
        if (!token) {
            notifications.show('Güvenlik token\'ı bulunamadı. Sayfayı yenileyin ve tekrar deneyin.', 'danger');
            return null;
        }

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
        const token = getAntiForgeryToken();
        if (!token) {
            notifications.show('Güvenlik token\'ı bulunamadı. Sayfayı yenileyin ve tekrar deneyin.', 'danger');
            return null;
        }

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

    /**
     * Belirli bir departman için içerik öğelerini getirir
     * @param {number} department - Departman ID
     * @param {Function} [callback] - Veriler yüklendikten sonra çağrılacak fonksiyon
     * @returns {Promise<Array>} İçerik öğeleri dizisi
     */
    async function getContentItemsByDepartment(department, callback) {
        return await getContentItems(callback, department);
    }

    return {
        call,
        getContentItems,
        getContentItemsByDepartment,
        updateCategory,
        updateSubCategory,
        addSubCategory,
        deleteCategory
    };
})();