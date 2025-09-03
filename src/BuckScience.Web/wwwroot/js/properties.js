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
        console.log('Initializing property features for property:', propertyId);
        
        const m = map();
        if (!m) {
            console.error('Map not available for initializing property features');
            return;
        }
        
        if (!propertyId) {
            console.error('Property ID is required for initializing property features');
            return;
        }

        // Store the current property ID
        window.App._currentPropertyId = propertyId;

        // Center map on property and fit to include cameras/features
        centerMapOnProperty();

        // Load and display existing features for this property
        loadPropertyFeatures(propertyId);

        // Set up drawing event handlers for features (ensure this is only done once)
        if (!window.App._featureDrawingSetup) {
            setupFeatureDrawing(propertyId);
            window.App._featureDrawingSetup = true;
        }
    };

    function centerMapOnProperty() {
        const m = map();
        if (!m) return;

        const propertyCoords = window.App?.propertyCoords;
        if (!propertyCoords) return;

        // Create a bounding box to include property location, cameras, and features
        let bounds = new mapboxgl.LngLatBounds();
        
        // Add property center to bounds
        bounds.extend([propertyCoords.lng, propertyCoords.lat]);

        // Add camera locations to bounds if available
        const cameras = getCameraLocations();
        cameras.forEach(camera => {
            bounds.extend([camera.lng, camera.lat]);
        });

        // If we have a valid bounds with multiple points, fit to bounds
        // Otherwise, just center on the property
        if (cameras.length > 0) {
            m.fitBounds(bounds, {
                padding: 50,
                maxZoom: 18,
                duration: 1200
            });
        } else {
            // Just center on property with a reasonable zoom level
            m.flyTo({
                center: [propertyCoords.lng, propertyCoords.lat],
                zoom: 16,
                duration: 1200
            });
        }
    }

    function getCameraLocations() {
        // Try to extract camera coordinates from the DOM
        const cameras = [];
        document.querySelectorAll('.camera-card').forEach(card => {
            const coordsText = card.querySelector('.card-text:has(.fa-map-marker-alt)')?.textContent;
            if (coordsText) {
                const match = coordsText.match(/(-?\d+\.\d+),\s*(-?\d+\.\d+)/);
                if (match) {
                    cameras.push({
                        lat: parseFloat(match[1]),
                        lng: parseFloat(match[2])
                    });
                }
            }
        });
        return cameras;
    }

    function loadPropertyFeatures(propertyId) {
        const m = map();
        if (!m) {
            console.error('Map not available for loading features');
            return;
        }

        console.log('Loading features for property:', propertyId);
        
        fetch(`/properties/${propertyId}/features`)
            .then(response => {
                console.log('Features API response status:', response.status);
                if (!response.ok) {
                    throw new Error(`Features API failed with status: ${response.status}`);
                }
                return response.json();
            })
            .then(features => {
                console.log('Loaded features:', features);
                displayFeaturesOnMap(features);
            })
            .catch(error => {
                console.error('Error loading property features:', error);
                // Don't show alert for debugging, just log the error
            });
    }

    function displayFeaturesOnMap(features) {
        const m = map();
        if (!m) {
            console.error('Map not available for displaying features');
            return;
        }

        console.log('Displaying features on map:', features);

        // Remove existing feature layers and source
        const featureLayerIds = ['property-features-fill', 'property-features-line', 'property-features-points'];
        featureLayerIds.forEach(layerId => {
            if (m.getLayer(layerId)) {
                console.log('Removing existing layer:', layerId);
                m.removeLayer(layerId);
            }
        });

        if (m.getSource('property-features')) {
            console.log('Removing existing features source');
            m.removeSource('property-features');
        }

        if (!features || features.length === 0) {
            console.log('No features to display');
            return;
        }

        // Convert features to GeoJSON
        const geojsonFeatures = features.map(feature => {
            const wkt = feature.geometryWkt;
            console.log('Processing feature WKT:', wkt);
            
            // Simple WKT parsing - in production, use a proper WKT parser
            let geometry;
            try {
                geometry = parseSimpleWKT(wkt);
                console.log('Parsed geometry:', geometry);
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

        console.log('Converted to GeoJSON features:', geojsonFeatures);

        if (geojsonFeatures.length === 0) {
            console.warn('No valid features after WKT parsing');
            return;
        }

        const geojson = {
            type: 'FeatureCollection',
            features: geojsonFeatures
        };

        console.log('Adding features source to map');
        
        // Add source
        m.addSource('property-features', {
            type: 'geojson',
            data: geojson
        });

        // Add layers for different geometry types
        console.log('Adding feature layers');
        
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

        // Remove any existing click handlers to prevent duplicates
        featureLayerIds.forEach(layerId => {
            m.off('click', layerId);
        });

        // Add click handlers
        featureLayerIds.forEach(layerId => {
            m.on('click', layerId, (e) => {
                const feature = e.features[0];
                showFeaturePopup(feature, e.lngLat);
            });
        });

        console.log('Features successfully added to map');

        // Update bounds to include features if this is the initial load
        updateBoundsWithFeatures(geojsonFeatures);
    }

    function updateBoundsWithFeatures(geojsonFeatures) {
        const m = map();
        if (!m || geojsonFeatures.length === 0) return;

        const propertyCoords = window.App?.propertyCoords;
        if (!propertyCoords) return;

        // Only update bounds if we haven't moved from the property center yet
        const currentCenter = m.getCenter();
        const propertyCenter = [propertyCoords.lng, propertyCoords.lat];
        const distance = Math.sqrt(
            Math.pow(currentCenter.lng - propertyCenter[0], 2) + 
            Math.pow(currentCenter.lat - propertyCenter[1], 2)
        );

        // If we're still close to the property center (haven't manually panned), include features in bounds
        if (distance < 0.01) { // Small threshold for "close to property center"
            let bounds = new mapboxgl.LngLatBounds();
            bounds.extend(propertyCenter);

            // Add camera locations
            const cameras = getCameraLocations();
            cameras.forEach(camera => {
                bounds.extend([camera.lng, camera.lat]);
            });

            // Add feature geometries to bounds
            geojsonFeatures.forEach(feature => {
                if (feature.geometry.type === 'Point') {
                    bounds.extend(feature.geometry.coordinates);
                } else if (feature.geometry.type === 'LineString') {
                    feature.geometry.coordinates.forEach(coord => bounds.extend(coord));
                } else if (feature.geometry.type === 'Polygon') {
                    feature.geometry.coordinates[0].forEach(coord => bounds.extend(coord));
                }
            });

            // Fit to bounds with cameras and features
            m.fitBounds(bounds, {
                padding: 50,
                maxZoom: 18,
                duration: 800
            });
        }
    }

    function setupFeatureDrawing(propertyId) {
        const m = map();
        if (!m || !window.MapboxDraw) return;

        // Use the stored draw instance
        const draw = window.App._draw;
        
        if (!draw) {
            console.warn('MapboxDraw control not available');
            return;
        }

        // Remove existing event listeners to prevent duplicates
        m.off('draw.create');
        m.off('draw.update');
        m.off('draw.delete');

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
        // Clear any drawing instructions
        const instructionDiv = document.getElementById('drawing-instructions');
        if (instructionDiv) {
            instructionDiv.remove();
        }
        
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

                // Reload property details view to refresh features list and count
                refreshPropertyDetailsView(propertyId);

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
                // Reload property details view to refresh features list and count
                const propertyId = window.App._currentPropertyId;
                if (propertyId) {
                    refreshPropertyDetailsView(propertyId);
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

    // Function to refresh the property details view after feature changes
    function refreshPropertyDetailsView(propertyId) {
        console.log('Refreshing property details view for property:', propertyId);
        
        // Use the sidebar loader to refresh the property details view
        if (window.App && window.App.loadSidebar) {
            const propertyDetailsUrl = `/properties/${propertyId}/details`;
            console.log('Loading sidebar with URL:', propertyDetailsUrl);
            
            window.App.loadSidebar(propertyDetailsUrl, { push: false }).then(() => {
                console.log('Property details view refreshed successfully');
                
                // After the view is reloaded, expand the features accordion to show the changes
                setTimeout(() => {
                    const featuresAccordion = document.getElementById('featuresCollapse');
                    if (featuresAccordion && !featuresAccordion.classList.contains('show')) {
                        console.log('Expanding features accordion');
                        // Use Bootstrap's collapse API to show the features section
                        const bsCollapse = new bootstrap.Collapse(featuresAccordion, {
                            show: true
                        });
                    }
                    
                    // Re-initialize property features for the refreshed view
                    if (window.App && window.App.initializePropertyFeatures) {
                        console.log('Re-initializing property features');
                        window.App.initializePropertyFeatures(propertyId);
                    }
                }, 200); // Small delay to ensure DOM is updated
            }).catch(error => {
                console.error('Error refreshing property details view:', error);
                // Fallback: just reload features if sidebar loader fails
                console.log('Falling back to reloading features only');
                loadPropertyFeatures(propertyId);
            });
        } else {
            console.warn('Sidebar loader not available, falling back to reloading features');
            // Fallback: just reload features if sidebar loader not available
            loadPropertyFeatures(propertyId);
        }
    }

    // Helper functions
    function parseSimpleWKT(wkt) {
        // Basic WKT parsing - this is simplified and should use a proper library in production
        if (!wkt || typeof wkt !== 'string') {
            throw new Error('Invalid WKT: must be a non-empty string');
        }
        
        wkt = wkt.trim();
        console.log('Parsing WKT:', wkt);
        
        if (wkt.startsWith('POINT(')) {
            const match = wkt.match(/POINT\s*\(\s*([^)]+)\s*\)/i);
            if (!match) throw new Error('Invalid POINT WKT format');
            
            const coords = match[1].trim().split(/\s+/);
            if (coords.length < 2) throw new Error('POINT must have at least 2 coordinates');
            
            return {
                type: 'Point',
                coordinates: [parseFloat(coords[0]), parseFloat(coords[1])]
            };
        } else if (wkt.startsWith('LINESTRING(')) {
            const match = wkt.match(/LINESTRING\s*\(\s*([^)]+)\s*\)/i);
            if (!match) throw new Error('Invalid LINESTRING WKT format');
            
            const coordString = match[1].trim();
            const coordinates = coordString.split(',').map(pair => {
                const coords = pair.trim().split(/\s+/);
                if (coords.length < 2) throw new Error('Each coordinate pair must have at least 2 values');
                return [parseFloat(coords[0]), parseFloat(coords[1])];
            });
            
            if (coordinates.length < 2) throw new Error('LINESTRING must have at least 2 coordinate pairs');
            
            return {
                type: 'LineString',
                coordinates: coordinates
            };
        } else if (wkt.startsWith('POLYGON(')) {
            const match = wkt.match(/POLYGON\s*\(\s*\(\s*([^)]+)\s*\)\s*\)/i);
            if (!match) throw new Error('Invalid POLYGON WKT format');
            
            const coordString = match[1].trim();
            const coordinates = coordString.split(',').map(pair => {
                const coords = pair.trim().split(/\s+/);
                if (coords.length < 2) throw new Error('Each coordinate pair must have at least 2 values');
                return [parseFloat(coords[0]), parseFloat(coords[1])];
            });
            
            if (coordinates.length < 3) throw new Error('POLYGON must have at least 3 coordinate pairs');
            
            // Ensure polygon is closed (first and last points are the same)
            const first = coordinates[0];
            const last = coordinates[coordinates.length - 1];
            if (first[0] !== last[0] || first[1] !== last[1]) {
                coordinates.push([first[0], first[1]]);
            }
            
            return {
                type: 'Polygon',
                coordinates: [coordinates]
            };
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