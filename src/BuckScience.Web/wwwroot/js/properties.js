// Property-specific UX: inject property lists/details into the sidebar,
// and interact with the map without reinitializing it.

window.App = window.App || {};

(function () {
    const map = () => window.App?.getMap?.() || window.App?.map;

    function getToken() {
        if (typeof mapboxgl !== 'undefined' && mapboxgl.accessToken) return mapboxgl.accessToken;
        const meta = document.querySelector('meta[name="mapbox-token"]');
        if (meta?.content) return meta.content;
        if (window.__MAPBOX_TOKEN) return window.__MAPBOX_TOKEN;
        return '';
    }

    // Fit to bbox helper
    function fitToBbox(bbox, options = {}) {
        const m = map();
        if (!m || !bbox || bbox.length !== 4) return;
        m.fitBounds([[bbox[0], bbox[1]], [bbox[2], bbox[3]]], {
            padding: 40,
            maxZoom: 13,
            duration: 1200,
            ...options
        });
    }

    // Mount geocoder and wire map <-> form
    function wirePropertyForm(container) {
        const m = map();
        if (!m || typeof mapboxgl === 'undefined') return;

        const latInput = container?.querySelector?.('#Latitude');
        const lngInput = container?.querySelector?.('#Longitude');
        const geocoderHost = container?.querySelector?.('#property-geocoder');
        if (!latInput || !lngInput) return; // not the create/edit view

        // Ensure one marker reused across loads
        if (!window.App._propMarker) {
            window.App._propMarker = new mapboxgl.Marker({ draggable: true });
        }
        const marker = window.App._propMarker;

        function setCoords(lat, lng, opts = {}) {
            if (!Number.isFinite(lat) || !Number.isFinite(lng)) return;
            latInput.value = Number(lat).toFixed(6);
            lngInput.value = Number(lng).toFixed(6);
            marker.setLngLat([lng, lat]).addTo(m);
            if (opts.fly) m.flyTo({ center: [lng, lat], zoom: Math.max(m.getZoom(), 14) });
        }

        // Initialize from inputs or map center
        const currentLat = parseFloat(latInput.value);
        const currentLng = parseFloat(lngInput.value);
        if (Number.isFinite(currentLat) && Number.isFinite(currentLng) && (currentLat !== 0 || currentLng !== 0)) {
            setCoords(currentLat, currentLng, { fly: true });
        } else {
            const c = m.getCenter();
            setCoords(c.lat, c.lng);
        }

        // Marker drag updates inputs
        if (marker._onDragEnd) marker.off('dragend', marker._onDragEnd);
        marker._onDragEnd = () => {
            const ll = marker.getLngLat();
            setCoords(ll.lat, ll.lng);
        };
        marker.on('dragend', marker._onDragEnd);

        // Map click sets coords
        if (window.App._propClickHandler) m.off('click', window.App._propClickHandler);
        window.App._propClickHandler = (e) => setCoords(e.lngLat.lat, e.lngLat.lng);
        m.on('click', window.App._propClickHandler);

        // Inputs change -> move marker
        if (latInput._onChange) latInput.removeEventListener('change', latInput._onChange);
        if (lngInput._onChange) lngInput.removeEventListener('change', lngInput._onChange);
        const onInputChange = () => {
            const lat = parseFloat(latInput.value);
            const lng = parseFloat(lngInput.value);
            if (Number.isFinite(lat) && Number.isFinite(lng)) marker.setLngLat([lng, lat]).addTo(m);
        };
        latInput._onChange = onInputChange;
        lngInput._onChange = onInputChange;
        latInput.addEventListener('change', onInputChange);
        lngInput.addEventListener('change', onInputChange);

        // Mount Geocoder into the view
        if (geocoderHost) {
            const token = getToken();
            if (typeof MapboxGeocoder !== 'undefined' && token) {
                geocoderHost.innerHTML = '';
                const geocoder = new MapboxGeocoder({
                    accessToken: token,
                    mapboxgl,
                    marker: false,
                    placeholder: 'Search address or place'
                });
                geocoder.on('result', (e) => {
                    if (Array.isArray(e?.result?.center)) {
                        const [lng, lat] = e.result.center;
                        setCoords(lat, lng, { fly: true });
                    }
                });
                geocoderHost.appendChild(geocoder.onAdd(m));
            } else if (!token) {
                console.warn('Mapbox Geocoder not initialized: missing access token.');
            }
        }
    }

    // Sidebar loads
    document.addEventListener('sidebar:loaded', () => {
        const container = document.getElementById('sidebar-content');
        if (!container) return;

        // Focus map by bbox links
        container.querySelectorAll('[data-bbox]').forEach((el) => {
            el.addEventListener('click', (e) => {
                const raw = el.getAttribute('data-bbox');
                if (!raw) return;
                const parts = raw.split(',').map(Number);
                if (parts.length === 4 && parts.every(Number.isFinite)) {
                    e.preventDefault();
                    fitToBbox(parts);
                }
            });
        });

        // AJAX forms (optional)
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

        // Wire the property create/edit form
        wirePropertyForm(container);
    });

    // Initial page load (SSR)
    document.addEventListener('DOMContentLoaded', () => {
        const container = document.getElementById('sidebar-content') || document.body;
        wirePropertyForm(container);
    });

    // Mount camera form and wire map <-> form with property recentering
    function wireCameraForm(container) {
        const m = map();
        if (!m || typeof mapboxgl === 'undefined') return;

        const latInput = container?.querySelector?.('#Latitude');
        const lngInput = container?.querySelector?.('#Longitude');
        if (!latInput || !lngInput) return; // not the camera create/edit view

        // Check if we have property coordinates for centering
        const propertyCoords = window.App?.propertyCoords;
        if (!propertyCoords) return;

        // Ensure one marker reused across loads
        if (!window.App._cameraMarker) {
            window.App._cameraMarker = new mapboxgl.Marker({ draggable: true, color: '#FF6B35' }); // Different color for camera
        }
        const marker = window.App._cameraMarker;

        function setCoords(lat, lng, opts = {}) {
            if (!Number.isFinite(lat) || !Number.isFinite(lng)) return;
            latInput.value = Number(lat).toFixed(6);
            lngInput.value = Number(lng).toFixed(6);
            marker.setLngLat([lng, lat]).addTo(m);
            if (opts.fly) m.flyTo({ center: [lng, lat], zoom: Math.max(m.getZoom(), 14) });
        }

        // First, recenter map to property location
        m.flyTo({ 
            center: [propertyCoords.lng, propertyCoords.lat], 
            zoom: Math.max(m.getZoom(), 14) 
        });

        // Initialize camera coordinates from inputs or property center
        const currentLat = parseFloat(latInput.value);
        const currentLng = parseFloat(lngInput.value);
        if (Number.isFinite(currentLat) && Number.isFinite(currentLng) && (currentLat !== 0 || currentLng !== 0)) {
            setCoords(currentLat, currentLng);
        } else {
            // Default to property center
            setCoords(propertyCoords.lat, propertyCoords.lng);
        }

        // Marker drag updates inputs
        if (marker._onDragEnd) marker.off('dragend', marker._onDragEnd);
        marker._onDragEnd = () => {
            const ll = marker.getLngLat();
            setCoords(ll.lat, ll.lng);
        };
        marker.on('dragend', marker._onDragEnd);

        // Map click sets coords
        if (window.App._cameraClickHandler) m.off('click', window.App._cameraClickHandler);
        window.App._cameraClickHandler = (e) => setCoords(e.lngLat.lat, e.lngLat.lng);
        m.on('click', window.App._cameraClickHandler);

        // Inputs change -> move marker
        if (latInput._onChange) latInput.removeEventListener('change', latInput._onChange);
        if (lngInput._onChange) lngInput.removeEventListener('change', lngInput._onChange);
        const onInputChange = () => {
            const lat = parseFloat(latInput.value);
            const lng = parseFloat(lngInput.value);
            if (Number.isFinite(lat) && Number.isFinite(lng)) marker.setLngLat([lng, lat]).addTo(m);
        };
        latInput._onChange = onInputChange;
        lngInput._onChange = onInputChange;
        latInput.addEventListener('change', onInputChange);
        lngInput.addEventListener('change', onInputChange);
    }

    // Public API
    window.App.focusProperty = function ({ bbox }) {
        if (bbox) fitToBbox(bbox);
    };

    // Expose wireCameraForm function
    window.App.wireCameraForm = wireCameraForm;
})();