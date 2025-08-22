// Initializes a single Mapbox map instance and reuses it.
// Token sources (in order):
// 1) <meta name="mapbox-token" content="pk.xxxxx">
// 2) <script src="~/js/map-init.js" data-mapbox-token="pk.xxxxx"></script>
// 3) window.__MAPBOX_TOKEN = "pk.xxxxx"
//
// Requirements:
// - Include Mapbox GL JS before this script.
// - Ensure a #map element exists on pages where you want the map.

window.App = window.App || {};

(function () {
    let initialized = false;
    let containerId = 'map';

    function getAccessToken() {
        // 1) Meta tag
        const meta = document.querySelector('meta[name="mapbox-token"]');
        if (meta?.content) return meta.content;

        // 2) Data attribute on this script tag
        const s = document.currentScript;
        const fromScript = s?.getAttribute('data-mapbox-token') || s?.dataset?.mapboxToken;
        if (fromScript) return fromScript;

        // 3) Global fallback
        if (window.__MAPBOX_TOKEN) return window.__MAPBOX_TOKEN;

        console.warn('Mapbox token not found. Provide it via a meta tag or data-mapbox-token.');
        return '';
    }

    function getContainerEl() {
        return document.getElementById(containerId);
    }

    App.setMapContainerId = function (id) {
        containerId = id || 'map';
    };

    App.getMap = function () {
        if (initialized && App.map) return App.map;

        const el = getContainerEl();
        if (!el) {
            // No map container on this page; do nothing.
            return undefined;
        }

        const token = getAccessToken();
        if (!token) {
            console.warn('Mapbox token is empty; map will not initialize.');
            return undefined;
        }

        if (typeof mapboxgl === 'undefined') {
            console.error('Mapbox GL JS not loaded. Include it before map-init.js.');
            return undefined;
        }

        mapboxgl.accessToken = token;

        const map = new mapboxgl.Map({
            container: el,
            style: 'mapbox://styles/mapbox/outdoors-v12',
            center: [-98.5795, 39.8283], // USA-ish center
            zoom: 4.5,
            minZoom: 3,
            maxZoom: 15,
            renderWorldCopies: false,
            cooperativeGestures: true,
            fadeDuration: 0
        });

        map.addControl(new mapboxgl.NavigationControl({ visualizePitch: true }), 'top-left');

        App.map = map;
        initialized = true;
        return map;
    };

    document.addEventListener('DOMContentLoaded', () => {
        App.getMap();
    });
})();