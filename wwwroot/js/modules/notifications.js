// wwwroot/js/modules/notifications.js
const notifications = (function() {
    /**
     * Kullanıcıya bildirim mesajı gösterir
     * @param {string} message - Gösterilecek mesaj
     * @param {string} type - Mesaj tipi (success, error, warning, info)
     * @param {number} duration - Mesajın görüntülenme süresi (ms)
     */
    function show(message, type = 'success', duration = 3000) {
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

    return {
        show
    };
})();