// Property-specific UX: inject property lists/details into the sidebar,
// and interact with the map without reinitializing it.

window.App = window.App || {};

(function () {
    const map = () => window.App?.getMap?.() || window.App?.map;

    // Utility: fit to bbox safely with a cap on zoom to keep tile usage modest
    function fitToBbox(bbox, options = {}) {
        const m = map();
        if (!m || !bbox || bbox.length !== 4) return;

        const bounds = [
            [bbox[0], bbox[1]],
            [bbox[2], bbox[3]]
        ];

        m.fitBounds(bounds, {
            padding: 40,
            maxZoom: 13, // cap so we don’t fetch ultra-high-zoom tiles
            duration: 1200,
            ...options
        });
    }

    // Listen for sidebar loads and auto-wire any property links/forms
    document.addEventListener('sidebar:loaded', () => {
        const container = document.getElementById('sidebar-content');
        if (!container) return;

        // Any link with data-sidebar-nav is already handled by sidebar.js, no change needed.

        // Links that carry a bbox to focus the map without reloading anything else
        container.querySelectorAll('[data-bbox]').forEach((el) => {
            el.addEventListener('click', (e) => {
                const raw = el.getAttribute('data-bbox'); // "minX,minY,maxX,maxY"
                if (!raw) return;
                const parts = raw.split(',').map(Number);
                if (parts.length === 4 && parts.every(Number.isFinite)) {
                    e.preventDefault();
                    fitToBbox(parts);
                }
            });
        });

        // Optional: intercept forms in the sidebar to submit via AJAX and re-render the sidebar only
        container.querySelectorAll('form[data-ajax]').forEach((form) => {
            form.addEventListener('submit', async (e) => {
                e.preventDefault();
                const action = form.getAttribute('action') || location.pathname;
                const method = (form.getAttribute('method') || 'GET').toUpperCase();
                const body = method === 'GET' ? null : new FormData(form);

                try {
                    const resp = await fetch(action, {
                        method,
                        headers: { 'X-Requested-With': 'XMLHttpRequest' },
                        body
                    });
                    if (!resp.ok) throw new Error(`Failed: ${resp.status}`);
                    const html = await resp.text();
                    document.getElementById('sidebar-content').innerHTML = html;
                    document.dispatchEvent(new CustomEvent('sidebar:loaded', { detail: { url: action } }));
                    history.pushState({ url: action }, '', action);
                } catch (err) {
                    console.error(err);
                    alert('Failed to submit form.');
                }
            });
        });
    });

    // Public API if you want to focus externally
    window.App.focusProperty = function ({ bbox }) {
        if (bbox) fitToBbox(bbox);
    };
})();