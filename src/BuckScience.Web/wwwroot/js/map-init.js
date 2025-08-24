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

    // Define available basemaps
    const basemaps = {
        satellite: {
            id: 'satellite',
            name: 'Satellite',
            style: 'mapbox://styles/mapbox/satellite-streets-v12',
            thumbnail: 'https://api.mapbox.com/styles/v1/mapbox/satellite-streets-v12/static/-98.5,39.8,3/100x70?access_token='
        },
        outdoors: {
            id: 'outdoors',
            name: 'Topo',
            style: 'mapbox://styles/mapbox/outdoors-v12',
            thumbnail: 'https://api.mapbox.com/styles/v1/mapbox/outdoors-v12/static/-98.5,39.8,3/100x70?access_token='
        }
    };

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

    // Create a custom basemap switcher control
    class BasemapSwitcherControl {
        constructor(styles) {
            this._styles = styles;
            this._container = document.createElement('div');
            this._container.className = 'mapboxgl-ctrl mapboxgl-ctrl-group';
            this._container.style.margin = '0 0 10px 0';
            this._flyout = null;
            this._activeStyle = 'satellite'; // Default active style
        }

        onAdd(map) {
            this._map = map;
            this._container.innerHTML = `
                <button class="basemap-toggle" title="Change basemap">
                    <svg viewBox="0 0 24 24" width="24" height="24">
                        <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm0 18c-4.41 0-8-3.59-8-8s3.59-8 8-8 8 3.59 8 8-3.59 8-8 8z"/>
                        <path d="M6.5 12h11" stroke="currentColor" stroke-width="2"/>
                        <path d="M12 6.5v11" stroke="currentColor" stroke-width="2"/>
                    </svg>
                </button>
            `;

            const token = getAccessToken();
            this._container.querySelector('button').addEventListener('click', () => {
                this._toggleFlyout(token);
            });

            document.addEventListener('click', (e) => {
                if (this._flyout && !this._container.contains(e.target)) {
                    this._closeFlyout();
                }
            });

            return this._container;
        }

        onRemove() {
            this._container.remove();
            this._map = undefined;
        }

        _toggleFlyout(token) {
            if (this._flyout) {
                this._closeFlyout();
                return;
            }

            this._flyout = document.createElement('div');
            this._flyout.className = 'basemap-flyout';
            this._flyout.style.cssText = `
                position: absolute;
                bottom: 40px;
                right: 0;
                background: white;
                border-radius: 4px;
                padding: 10px;
                box-shadow: 0 0 10px rgba(0,0,0,0.3);
                z-index: 1;
                width: 230px;
            `;

            let html = '<div style="display: grid; grid-template-columns: 1fr 1fr; gap: 10px;">';

            for (const id in this._styles) {
                const style = this._styles[id];
                const isActive = id === this._activeStyle;
                const thumbnailUrl = style.thumbnail + token;

                html += `
                    <div class="basemap-option ${isActive ? 'active' : ''}" data-style="${id}" style="cursor: pointer; ${isActive ? 'border: 2px solid #4264fb;' : 'border: 2px solid transparent;'} border-radius: 4px; overflow: hidden;">
                        <img src="${thumbnailUrl}" alt="${style.name}" style="width: 100%; height: 70px; object-fit: cover;">
                        <div style="text-align: center; padding: 5px; font-size: 12px;">${style.name}</div>
                    </div>
                `;
            }

            html += '</div>';
            this._flyout.innerHTML = html;

            this._flyout.querySelectorAll('.basemap-option').forEach(el => {
                el.addEventListener('click', (e) => {
                    const styleId = el.getAttribute('data-style');
                    if (styleId !== this._activeStyle) {
                        this._map.setStyle(this._styles[styleId].style);
                        this._activeStyle = styleId;
                    }
                    this._closeFlyout();
                });
            });

            this._container.appendChild(this._flyout);
        }

        _closeFlyout() {
            if (this._flyout) {
                this._flyout.remove();
                this._flyout = null;
            }
        }
    }

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
            style: 'mapbox://styles/mapbox/satellite-streets-v12',
            center: [-98.5795, 39.8283], // USA-ish center
            zoom: 4.5,
            minZoom: 3,
            maxZoom: 15,
            renderWorldCopies: false,
            cooperativeGestures: true,
            fadeDuration: 0
        });

        // Add drawing and zoom controls
        if (typeof MapboxDraw !== 'undefined') {
            const draw = new MapboxDraw({
                displayControlsDefault: true,
                controls: {
                    polygon: true,
                    line: true,
                    point: true,
                    trash: true
                }
            });
            map.addControl(draw, 'top-right');
        } else {
            console.warn('MapboxDraw not loaded. Include it to enable drawing features.');
        }

        map.addControl(new mapboxgl.NavigationControl({ visualizePitch: true }), 'top-right');

        // Add the basemap switcher control
        map.addControl(new BasemapSwitcherControl(basemaps), 'bottom-right');

        App.map = map;
        initialized = true;
        return map;
    };

    document.addEventListener('DOMContentLoaded', () => {
        App.getMap();
    });
})();