// wwwroot/js/modules/notifications.js
const notifications = (function () {
    /**
     * Kullanıcıya bildirim mesajı gösterir
     * @param {string} message - Gösterilecek mesaj
     * @param {string} type - Mesaj tipi (success, error, warning, info)
     * @param {number} duration - Mesajın görüntülenme süresi (ms)
     */
    function show(message, type = 'success', duration = 3000) {
        if (!message) return; // Boş mesajlar için gösterme

        // Map any alternative type names to Bootstrap's alert types
        const typeMap = {
            'error': 'danger',
            'info': 'info',
            'warning': 'warning',
            'success': 'success',
            'danger': 'danger'
        };

        const alertType = typeMap[type] || 'info';

        const alertDiv = document.createElement('div');
        alertDiv.className = `alert alert-${alertType} alert-dismissible fade show`;
        alertDiv.role = "alert";
        alertDiv.style.marginBottom = '10px';

        // Add ARIA for accessibility
        alertDiv.setAttribute('aria-live', 'polite');
        alertDiv.setAttribute('aria-atomic', 'true');

        alertDiv.innerHTML = message +
            '<button type="button" class="close" data-dismiss="alert" aria-label="Kapat">' +
            '<span aria-hidden="true">&times;</span></button>';

        // Mevcut bildirimleri kaldır
        const existingAlerts = document.querySelectorAll('.alert');
        existingAlerts.forEach(alert => alert.remove());

        // Yeni bildirimi ekle - keep the original insertion approach
        const container = document.querySelector('.container, .container-fluid');
        if (container) {
            container.insertBefore(alertDiv, container.firstChild);

            // Add simple fade-in animation
            alertDiv.style.opacity = '0';
            alertDiv.style.transition = 'opacity 0.3s ease';

            // Trigger reflow for animation
            setTimeout(function () {
                alertDiv.style.opacity = '1';
            }, 10);

            if (duration > 0) {
                setTimeout(function () {
                    // Add fade-out before removing
                    alertDiv.style.opacity = '0';
                    setTimeout(function () {
                        if (alertDiv.parentNode) {
                            alertDiv.remove();
                        }
                    }, 300);
                }, duration);
            }
        }
    }

    return {
        show
    };
})();