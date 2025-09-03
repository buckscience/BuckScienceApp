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
            maxZoom: 18,
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
            if (opts.fly) m.flyTo({ center: [lng, lat], zoom: Math.max(m.getZoom(), 18) });
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

    // Property Features functionality
    window.App.initializePropertyFeatures = function(propertyId) {
        const m = map();
        if (!m || !propertyId) return;

        // Store the current property ID
        window.App._currentPropertyId = propertyId;

        // Load and display existing features for this property
        loadPropertyFeatures(propertyId);

        // Set up drawing event handlers for features
        setupFeatureDrawing(propertyId);
    };

    function loadPropertyFeatures(propertyId) {
        const m = map();
        if (!m) return;

        fetch(`/properties/${propertyId}/features`)
            .then(response => response.json())
            .then(features => {
                displayFeaturesOnMap(features);
            })
            .catch(error => {
                console.error('Error loading property features:', error);
            });
    }

    function displayFeaturesOnMap(features) {
        const m = map();
        if (!m) return;

        // Remove existing feature layers
        const featureLayerIds = ['property-features-fill', 'property-features-line', 'property-features-points'];
        featureLayerIds.forEach(layerId => {
            if (m.getLayer(layerId)) {
                m.removeLayer(layerId);
            }
        });

        if (m.getSource('property-features')) {
            m.removeSource('property-features');
        }

        if (features.length === 0) return;

        // Convert features to GeoJSON
        const geojsonFeatures = features.map(feature => {
            const wkt = feature.geometryWkt;
            // Simple WKT parsing - in production, use a proper WKT parser
            let geometry;
            try {
                geometry = parseSimpleWKT(wkt);
            } catch (e) {
                console.warn('Could not parse WKT:', wkt, e);
                return null;
            }

            return {
                type: 'Feature',
                properties: {
                    id: feature.id,
                    classificationType: feature.classificationType,
                    notes: feature.notes,
                    createdAt: feature.createdAt,
                    name: getFeatureName(feature.classificationType),
                    color: getFeatureColor(feature.classificationType)
                },
                geometry: geometry
            };
        }).filter(f => f !== null);

        const geojson = {
            type: 'FeatureCollection',
            features: geojsonFeatures
        };

        // Add source
        m.addSource('property-features', {
            type: 'geojson',
            data: geojson
        });

        // Add layers for different geometry types
        m.addLayer({
            id: 'property-features-fill',
            type: 'fill',
            source: 'property-features',
            filter: ['==', ['geometry-type'], 'Polygon'],
            paint: {
                'fill-color': ['get', 'color'],
                'fill-opacity': 0.3
            }
        });

        m.addLayer({
            id: 'property-features-line',
            type: 'line',
            source: 'property-features',
            filter: ['in', ['geometry-type'], ['literal', ['LineString', 'Polygon']]],
            paint: {
                'line-color': ['get', 'color'],
                'line-width': 3
            }
        });

        m.addLayer({
            id: 'property-features-points',
            type: 'circle',
            source: 'property-features',
            filter: ['==', ['geometry-type'], 'Point'],
            paint: {
                'circle-color': ['get', 'color'],
                'circle-radius': 8,
                'circle-stroke-color': '#ffffff',
                'circle-stroke-width': 2
            }
        });

        // Add click handlers
        ['property-features-fill', 'property-features-line', 'property-features-points'].forEach(layerId => {
            m.on('click', layerId, (e) => {
                const feature = e.features[0];
                showFeaturePopup(feature, e.lngLat);
            });
        });
    }

    function setupFeatureDrawing(propertyId) {
        const m = map();
        if (!m || !window.MapboxDraw) return;

        // Access the draw control from the map
        const controls = m._controls || [];
        let draw = null;
        for (const control of controls) {
            if (control.control && control.control.getMode) {
                draw = control.control;
                break;
            }
        }

        if (!draw) {
            console.warn('MapboxDraw control not found');
            return;
        }

        // Store the draw instance
        window.App._draw = draw;

        // Listen for draw events
        m.on('draw.create', (e) => {
            handleFeatureCreated(e.features[0], propertyId);
        });

        m.on('draw.update', (e) => {
            handleFeatureUpdated(e.features[0], propertyId);
        });

        m.on('draw.delete', (e) => {
            handleFeatureDeleted(e.features, propertyId);
        });
    }

    function handleFeatureCreated(feature, propertyId) {
        // Show a modal to select feature type and add notes
        showFeatureTypeModal(feature, propertyId, 'create');
    }

    function handleFeatureUpdated(feature, propertyId) {
        // Handle feature updates (geometry changes)
        console.log('Feature updated:', feature);
    }

    function handleFeatureDeleted(features, propertyId) {
        // Handle feature deletion
        features.forEach(feature => {
            console.log('Feature deleted:', feature);
        });
    }

    function showFeatureTypeModal(feature, propertyId, mode = 'create') {
        const modalHtml = `
            <div class="modal fade" id="featureTypeModal" tabindex="-1">
                <div class="modal-dialog">
                    <div class="modal-content">
                        <div class="modal-header">
                            <h5 class="modal-title">${mode === 'create' ? 'Add' : 'Edit'} Property Feature</h5>
                            <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                        </div>
                        <div class="modal-body">
                            <form id="featureForm">
                                <div class="mb-3">
                                    <label for="featureType" class="form-label">Feature Type</label>
                                    <select class="form-select" id="featureType" required>
                                        <option value="1">Bedding Area</option>
                                        <option value="2">Feeding Zone</option>
                                        <option value="3">Travel Corridor</option>
                                        <option value="4">Pinch Point/Funnel</option>
                                        <option value="5">Water Source</option>
                                        <option value="6">Security Cover</option>
                                        <option value="7">Other</option>
                                    </select>
                                </div>
                                <div class="mb-3">
                                    <label for="featureNotes" class="form-label">Notes (optional)</label>
                                    <textarea class="form-control" id="featureNotes" rows="3" placeholder="Add any notes about this feature..."></textarea>
                                </div>
                            </form>
                        </div>
                        <div class="modal-footer">
                            <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
                            <button type="button" class="btn btn-primary" onclick="savePropertyFeature()">Save Feature</button>
                        </div>
                    </div>
                </div>
            </div>
        `;

        // Remove existing modal if any
        const existingModal = document.getElementById('featureTypeModal');
        if (existingModal) {
            existingModal.remove();
        }

        // Add modal to DOM
        document.body.insertAdjacentHTML('beforeend', modalHtml);

        // Store feature data
        window.App._tempFeature = feature;
        window.App._tempPropertyId = propertyId;
        window.App._tempMode = mode;

        // Show modal
        const modal = new bootstrap.Modal(document.getElementById('featureTypeModal'));
        modal.show();
    }

    window.App.savePropertyFeature = function() {
        const feature = window.App._tempFeature;
        const propertyId = window.App._tempPropertyId;
        const mode = window.App._tempMode;

        const featureType = parseInt(document.getElementById('featureType').value);
        const notes = document.getElementById('featureNotes').value.trim() || null;

        // Convert GeoJSON geometry to WKT
        const geometryWkt = geometryToWKT(feature.geometry);

        const data = {
            classificationType: featureType,
            geometryWkt: geometryWkt,
            notes: notes
        };

        const url = `/properties/${propertyId}/features`;
        const method = 'POST';

        fetch(url, {
            method: method,
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
            },
            body: JSON.stringify(data)
        })
        .then(response => {
            if (response.ok) {
                // Close modal
                const modal = bootstrap.Modal.getInstance(document.getElementById('featureTypeModal'));
                modal.hide();

                // Clear the draw feature
                if (window.App._draw) {
                    window.App._draw.delete(feature.id);
                }

                // Reload features
                loadPropertyFeatures(propertyId);

                // Show success message
                console.log('Feature saved successfully');
            } else {
                throw new Error('Failed to save feature');
            }
        })
        .catch(error => {
            console.error('Error saving feature:', error);
            alert('Error saving feature. Please try again.');
        });
    };

    function showFeaturePopup(feature, lngLat) {
        const m = map();
        if (!m) return;

        const props = feature.properties;
        const popupHtml = `
            <div style="max-width: 200px;">
                <h6>${props.name}</h6>
                ${props.notes ? `<p class="small text-muted">${props.notes}</p>` : ''}
                <div class="d-flex gap-2 mt-2">
                    <button class="btn btn-sm btn-outline-primary" onclick="editPropertyFeature(${props.id})">Edit</button>
                    <button class="btn btn-sm btn-outline-danger" onclick="deletePropertyFeature(${props.id})">Delete</button>
                </div>
            </div>
        `;

        new mapboxgl.Popup()
            .setLngLat(lngLat)
            .setHTML(popupHtml)
            .addTo(m);
    }

    window.App.editPropertyFeature = function(featureId) {
        // TODO: Implement feature editing
        console.log('Edit feature:', featureId);
    };

    window.App.deletePropertyFeature = function(featureId) {
        if (!confirm('Are you sure you want to delete this feature?')) {
            return;
        }

        fetch(`/features/${featureId}`, {
            method: 'DELETE',
            headers: {
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
            }
        })
        .then(response => {
            if (response.ok) {
                // Reload features
                const propertyId = window.App._currentPropertyId;
                if (propertyId) {
                    loadPropertyFeatures(propertyId);
                }
                console.log('Feature deleted successfully');
            } else {
                throw new Error('Failed to delete feature');
            }
        })
        .catch(error => {
            console.error('Error deleting feature:', error);
            alert('Error deleting feature. Please try again.');
        });
    };

    // Helper functions
    function parseSimpleWKT(wkt) {
        // Basic WKT parsing - this is simplified and should use a proper library in production
        if (wkt.startsWith('POINT(')) {
            const coords = wkt.match(/POINT\(([^)]+)\)/)[1].split(' ');
            return {
                type: 'Point',
                coordinates: [parseFloat(coords[0]), parseFloat(coords[1])]
            };
        } else if (wkt.startsWith('LINESTRING(')) {
            const coordString = wkt.match(/LINESTRING\(([^)]+)\)/)[1];
            const coordinates = coordString.split(',').map(pair => {
                const coords = pair.trim().split(' ');
                return [parseFloat(coords[0]), parseFloat(coords[1])];
            });
            return {
                type: 'LineString',
                coordinates: coordinates
            };
        } else if (wkt.startsWith('POLYGON(')) {
            const match = wkt.match(/POLYGON\(\(([^)]+)\)\)/);
            if (match) {
                const coordString = match[1];
                const coordinates = coordString.split(',').map(pair => {
                    const coords = pair.trim().split(' ');
                    return [parseFloat(coords[0]), parseFloat(coords[1])];
                });
                return {
                    type: 'Polygon',
                    coordinates: [coordinates]
                };
            }
        }
        throw new Error('Unsupported WKT format: ' + wkt);
    }

    function geometryToWKT(geometry) {
        if (geometry.type === 'Point') {
            return `POINT(${geometry.coordinates[0]} ${geometry.coordinates[1]})`;
        } else if (geometry.type === 'LineString') {
            const coords = geometry.coordinates.map(coord => `${coord[0]} ${coord[1]}`).join(',');
            return `LINESTRING(${coords})`;
        } else if (geometry.type === 'Polygon') {
            const coords = geometry.coordinates[0].map(coord => `${coord[0]} ${coord[1]}`).join(',');
            return `POLYGON((${coords}))`;
        }
        throw new Error('Unsupported geometry type: ' + geometry.type);
    }

    function getFeatureName(classificationType) {
        const names = {
            1: 'Bedding Area',
            2: 'Feeding Zone',
            3: 'Travel Corridor',
            4: 'Pinch Point/Funnel',
            5: 'Water Source',
            6: 'Security Cover',
            7: 'Other'
        };
        return names[classificationType] || 'Unknown';
    }

    function getFeatureColor(classificationType) {
        const colors = {
            1: '#8B4513', // Brown for bedding
            2: '#32CD32', // Green for feeding
            3: '#FF6347', // Red for travel corridor
            4: '#FF8C00', // Orange for pinch points
            5: '#1E90FF', // Blue for water
            6: '#228B22', // Dark green for cover
            7: '#9370DB'  // Purple for other
        };
        return colors[classificationType] || '#999999';
    }

    // Expose property features functions globally
    window.savePropertyFeature = function() {
        window.App.savePropertyFeature();
    };
    
    window.editPropertyFeature = function(featureId) {
        window.App.editPropertyFeature(featureId);
    };
    
    window.deletePropertyFeature = function(featureId) {
        window.App.deletePropertyFeature(featureId);
    };

    // Expose wireCameraForm function
    window.App.wireCameraForm = wireCameraForm;
})();