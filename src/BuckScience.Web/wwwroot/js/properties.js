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
            maxZoom: 19,
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
                
                // Show loading state
                const submitBtn = form.querySelector('button[type="submit"]');
                const originalBtnText = submitBtn ? submitBtn.innerHTML : '';
                if (submitBtn) {
                    submitBtn.disabled = true;
                    submitBtn.innerHTML = '<i class="fas fa-spinner fa-spin me-1"></i>Saving...';
                }
                
                try {
                    const resp = await fetch(action, {
                        method,
                        headers: { 'X-Requested-With': 'XMLHttpRequest' },
                        body
                    });
                    
                    if (!resp.ok) {
                        throw new Error(`Failed: ${resp.status} ${resp.statusText}`);
                    }
                    
                    // Check if this is a redirect response
                    const contentType = resp.headers.get('content-type');
                    if (resp.redirected || (resp.url && resp.url !== action)) {
                        // Handle redirected response by loading the redirected URL in sidebar
                        const redirectUrl = resp.url;
                        console.log('Form submission redirected to:', redirectUrl);
                        if (window.App && window.App.loadSidebar) {
                            await window.App.loadSidebar(redirectUrl, { push: true });
                        } else {
                            window.location.href = redirectUrl;
                        }
                    } else {
                        // Handle regular response by replacing content
                        const html = await resp.text();
                        document.getElementById('sidebar-content').innerHTML = html;
                        document.dispatchEvent(new CustomEvent('sidebar:loaded', { detail: { url: action } }));
                        history.pushState({ url: action }, '', action);
                    }
                } catch (err) {
                    console.error('Form submission error:', err);
                    window.App.showModal('Error', 'Failed to submit form: ' + err.message, 'error');
                } finally {
                    // Restore button state
                    if (submitBtn) {
                        submitBtn.disabled = false;
                        submitBtn.innerHTML = originalBtnText;
                    }
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

        // Load and display existing features for this property
        loadPropertyFeatures(propertyId);

        // Display cameras on the map 
        displayCamerasOnMap();

        // Set up drawing event handlers for features (ensure this is only done once)
        if (!window.App._featureDrawingSetup) {
            setupFeatureDrawing(propertyId);
            window.App._featureDrawingSetup = true;
        }

        // After a short delay, calculate comprehensive bounds including all elements
        setTimeout(() => {
            calculateComprehensiveBounds();
        }, 1500); // Wait for features and cameras to load
    };

    function calculateComprehensiveBounds() {
        const m = map();
        if (!m) return;

        const propertyCoords = window.App?.propertyCoords;
        if (!propertyCoords) return;

        console.log('Calculating comprehensive bounds for map');

        // Create bounds and always include property location
        let bounds = new mapboxgl.LngLatBounds();
        bounds.extend([propertyCoords.lng, propertyCoords.lat]);
        let hasAdditionalPoints = false;

        // Add all camera markers to bounds
        if (window.App._cameraMarkers && window.App._cameraMarkers.length > 0) {
            console.log('Adding', window.App._cameraMarkers.length, 'cameras to bounds');
            window.App._cameraMarkers.forEach(marker => {
                const lngLat = marker.getLngLat();
                bounds.extend([lngLat.lng, lngLat.lat]);
                hasAdditionalPoints = true;
            });
        }

        // Add all features to bounds
        const featuresSource = m.getSource('property-features');
        if (featuresSource && featuresSource._data && featuresSource._data.features) {
            const features = featuresSource._data.features;
            console.log('Adding', features.length, 'features to bounds');
            
            features.forEach(feature => {
                if (feature.geometry.type === 'Point') {
                    bounds.extend(feature.geometry.coordinates);
                    hasAdditionalPoints = true;
                } else if (feature.geometry.type === 'LineString') {
                    feature.geometry.coordinates.forEach(coord => {
                        bounds.extend(coord);
                        hasAdditionalPoints = true;
                    });
                } else if (feature.geometry.type === 'Polygon') {
                    feature.geometry.coordinates[0].forEach(coord => {
                        bounds.extend(coord);
                        hasAdditionalPoints = true;
                    });
                }
            });
        }

        // Apply bounds or center on property
        if (hasAdditionalPoints) {
            console.log('Fitting bounds to include all cameras and features');
            m.fitBounds(bounds, {
                padding: {
                    top: 60,
                    bottom: 60,
                    left: 60,
                    right: 60
                },
                maxZoom: 18, // More zoomed in for better view
                duration: 1500
            });
        } else {
            console.log('No additional points found, centering on property');
            m.flyTo({
                center: [propertyCoords.lng, propertyCoords.lat],
                zoom: 16,
                duration: 1500
            });
        }
    }

    function centerMapOnProperty() {
        const m = map();
        if (!m) return;

        const propertyCoords = window.App?.propertyCoords;
        if (!propertyCoords) return;

        // Center on property with a reasonable zoom level
        m.flyTo({
            center: [propertyCoords.lng, propertyCoords.lat],
            zoom: 16,
            duration: 1200
        });
    }

    function loadPropertyFeatures(propertyId) {
        const m = map();
        if (!m) {
            console.error('Map not available for loading features');
            return;
        }

        console.log('Loading features for property:', propertyId);
        
        // Show loading indicator
        showFeatureLoadingIndicator(true);
        
        fetch(`/properties/${propertyId}/features`)
            .then(response => {
                console.log('Features API response status:', response.status, response.statusText);
                if (!response.ok) {
                    return response.text().then(errorText => {
                        console.error('Features API error response:', errorText);
                        throw new Error(`Features API failed with status: ${response.status} - ${errorText}`);
                    });
                }
                return response.json();
            })
            .then(features => {
                console.log('Loaded features from API:', features);
                showFeatureLoadingIndicator(false);
                
                if (!Array.isArray(features)) {
                    console.error('Features response is not an array:', features);
                    throw new Error('Invalid features response format');
                }
                
                displayFeaturesOnMap(features);
            })
            .catch(error => {
                console.error('Error loading property features:', error);
                showFeatureLoadingIndicator(false);
                
                // Show user-friendly error message
                showFeatureError('Failed to load property features: ' + error.message);
            });
    }

    function showFeatureLoadingIndicator(show) {
        let indicator = document.getElementById('feature-loading-indicator');
        
        if (show) {
            if (!indicator) {
                indicator = document.createElement('div');
                indicator.id = 'feature-loading-indicator';
                indicator.className = 'alert alert-info position-fixed';
                indicator.style.cssText = 'top: 20px; left: 20px; z-index: 1000; max-width: 250px;';
                indicator.innerHTML = '<i class="fas fa-spinner fa-spin me-2"></i>Loading features...';
                document.body.appendChild(indicator);
            }
        } else {
            if (indicator) {
                indicator.remove();
            }
        }
    }

    function showFeatureError(message) {
        // Remove any existing error
        const existingError = document.getElementById('feature-error-indicator');
        if (existingError) {
            existingError.remove();
        }
        
        const errorDiv = document.createElement('div');
        errorDiv.id = 'feature-error-indicator';
        errorDiv.className = 'alert alert-danger position-fixed';
        errorDiv.style.cssText = 'top: 20px; left: 20px; z-index: 1000; max-width: 300px;';
        errorDiv.innerHTML = `
            <div class="d-flex justify-content-between align-items-start">
                <div>
                    <strong>Feature Error</strong><br>
                    <small>${message}</small>
                </div>
                <button type="button" class="btn-close btn-close-sm" onclick="this.parentElement.parentElement.remove()"></button>
            </div>
        `;
        document.body.appendChild(errorDiv);
        
        // Auto-remove after 10 seconds
        setTimeout(() => {
            if (errorDiv.parentElement) {
                errorDiv.remove();
            }
        }, 10000);
    }

    function displayFeaturesOnMap(features) {
        const m = map();
        if (!m) {
            console.error('Map not available for displaying features');
            showFeatureError('Map not available for displaying features');
            return;
        }

        console.log('Displaying features on map. Total features received:', features?.length || 0);

        // Remove existing feature layers and source
        const featureLayerIds = ['property-features-fill', 'property-features-line', 'property-features-points'];
        featureLayerIds.forEach(layerId => {
            if (m.getLayer(layerId)) {
                console.log('Removing existing layer:', layerId);
                try {
                    m.removeLayer(layerId);
                } catch (e) {
                    console.warn('Error removing layer:', layerId, e);
                }
            }
        });

        if (m.getSource('property-features')) {
            console.log('Removing existing features source');
            try {
                m.removeSource('property-features');
            } catch (e) {
                console.warn('Error removing source:', e);
            }
        }

        if (!features || !Array.isArray(features) || features.length === 0) {
            console.log('No features to display');
            return;
        }

        console.log('Processing', features.length, 'features for map display');

        // Convert features to GeoJSON
        const geojsonFeatures = [];
        const failedFeatures = [];

        features.forEach((feature, index) => {
            console.log(`Processing feature ${index + 1}/${features.length}:`, feature);
            
            if (!feature || typeof feature !== 'object') {
                console.warn('Invalid feature object at index', index, feature);
                failedFeatures.push({ index, reason: 'Invalid feature object', feature });
                return;
            }

            const wkt = feature.geometryWkt;
            if (!wkt) {
                console.warn('Feature missing geometryWkt at index', index, feature);
                failedFeatures.push({ index, reason: 'Missing geometryWkt', feature });
                return;
            }

            console.log(`Processing feature ${index + 1} WKT:`, wkt);
            
            let geometry;
            try {
                geometry = parseSimpleWKT(wkt);
                console.log(`Parsed geometry for feature ${index + 1}:`, geometry);
            } catch (e) {
                console.warn(`Could not parse WKT for feature ${index + 1}:`, wkt, e);
                failedFeatures.push({ index, reason: 'WKT parsing failed: ' + e.message, feature, wkt });
                return;
            }

            if (!geometry || !geometry.type || !geometry.coordinates) {
                console.warn(`Invalid geometry result for feature ${index + 1}:`, geometry);
                failedFeatures.push({ index, reason: 'Invalid geometry result', feature, geometry });
                return;
            }

            const geoJsonFeature = {
                type: 'Feature',
                properties: {
                    id: feature.id,
                    classificationType: feature.classificationType,
                    notes: feature.notes || '',
                    createdAt: feature.createdAt,
                    name: getFeatureName(feature.classificationType),
                    color: getFeatureColor(feature.classificationType)
                },
                geometry: geometry
            };

            geojsonFeatures.push(geoJsonFeature);
            console.log(`Successfully processed feature ${index + 1}:`, geoJsonFeature);
        });

        console.log(`Conversion complete. Successfully processed: ${geojsonFeatures.length}, Failed: ${failedFeatures.length}`);
        
        if (failedFeatures.length > 0) {
            console.warn('Failed to process some features:', failedFeatures);
            showFeatureError(`Warning: ${failedFeatures.length} feature(s) could not be displayed. Check console for details.`);
        }

        if (geojsonFeatures.length === 0) {
            console.warn('No valid features after processing');
            if (failedFeatures.length > 0) {
                showFeatureError('No features could be displayed. All features failed to parse. Check console for details.');
            }
            return;
        }

        const geojson = {
            type: 'FeatureCollection',
            features: geojsonFeatures
        };

        console.log('Adding features source to map with', geojsonFeatures.length, 'features');
        
        try {
            // Add source
            m.addSource('property-features', {
                type: 'geojson',
                data: geojson
            });

            console.log('Features source added successfully');

            // Add layers for different geometry types
            console.log('Adding feature layers');
            
            // Polygon fill layer
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

            // Line layer (for LineStrings and Polygon outlines)
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

            // Point layer
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

            console.log('All feature layers added successfully');

            // Remove any existing click handlers to prevent duplicates
            featureLayerIds.forEach(layerId => {
                m.off('click', layerId);
            });

            // Add click handlers
            featureLayerIds.forEach(layerId => {
                m.on('click', layerId, (e) => {
                    if (e.features && e.features.length > 0) {
                        const feature = e.features[0];
                        showFeaturePopup(feature, e.lngLat);
                    }
                });
                
                // Change cursor on hover
                m.on('mouseenter', layerId, () => {
                    m.getCanvas().style.cursor = 'pointer';
                });
                
                m.on('mouseleave', layerId, () => {
                    m.getCanvas().style.cursor = '';
                });
            });

            console.log('Feature interaction handlers added successfully');
            console.log('Features successfully added to map');

            // Update bounds to include features if this is the initial load
            updateBoundsWithFeatures(geojsonFeatures);
            
        } catch (error) {
            console.error('Error adding features to map:', error);
            showFeatureError('Failed to display features on map: ' + error.message);
        }
    }

    function updateBoundsWithFeatures(geojsonFeatures) {
        const m = map();
        if (!m) return;

        const propertyCoords = window.App?.propertyCoords;
        if (!propertyCoords) return;

        // Create bounds and always include property location
        let bounds = new mapboxgl.LngLatBounds();
        bounds.extend([propertyCoords.lng, propertyCoords.lat]);

        // Add all feature geometries to bounds
        if (geojsonFeatures && geojsonFeatures.length > 0) {
            geojsonFeatures.forEach(feature => {
                if (feature.geometry.type === 'Point') {
                    bounds.extend(feature.geometry.coordinates);
                } else if (feature.geometry.type === 'LineString') {
                    feature.geometry.coordinates.forEach(coord => bounds.extend(coord));
                } else if (feature.geometry.type === 'Polygon') {
                    feature.geometry.coordinates[0].forEach(coord => bounds.extend(coord));
                }
            });
        }

        // Add all camera markers to bounds
        if (window.App._cameraMarkers && window.App._cameraMarkers.length > 0) {
            window.App._cameraMarkers.forEach(marker => {
                const lngLat = marker.getLngLat();
                bounds.extend([lngLat.lng, lngLat.lat]);
            });
        }

        // Only update bounds if we have more than just the property location
        const boundsArray = bounds.toArray();
        const hasMultiplePoints = (boundsArray[0][0] !== boundsArray[1][0]) || (boundsArray[0][1] !== boundsArray[1][1]);
        
        if (hasMultiplePoints) {
            // Fit to bounds with all elements included
            m.fitBounds(bounds, {
                padding: {
                    top: 50,
                    bottom: 50,
                    left: 50,
                    right: 50
                },
                maxZoom: 18, // More zoomed in for better view
                duration: 1200
            });
        } else {
            // If only property location, use a reasonable zoom level
            m.flyTo({
                center: [propertyCoords.lng, propertyCoords.lat],
                zoom: 16,
                duration: 1200
            });
        }
    }

    function displayCamerasOnMap() {
        const m = map();
        if (!m) {
            console.error('Map not available for displaying cameras');
            return;
        }

        const propertyId = window.App._currentPropertyId;
        if (!propertyId) {
            console.error('Property ID not available for loading cameras');
            return;
        }

        console.log('Loading cameras for property:', propertyId);

        // Remove existing camera markers
        if (window.App._cameraMarkers) {
            window.App._cameraMarkers.forEach(marker => {
                marker.remove();
            });
            window.App._cameraMarkers = [];
        } else {
            window.App._cameraMarkers = [];
        }

        // Fetch camera data from API
        fetch(`/properties/${propertyId}/cameras/api`)
            .then(response => {
                console.log('Cameras API response status:', response.status, response.statusText);
                if (!response.ok) {
                    throw new Error(`Cameras API failed with status: ${response.status}`);
                }
                return response.json();
            })
            .then(cameras => {
                console.log('Loaded cameras from API:', cameras);
                
                if (!Array.isArray(cameras)) {
                    console.error('Cameras response is not an array:', cameras);
                    return;
                }

                if (cameras.length === 0) {
                    console.log('No cameras to display');
                    return;
                }

                console.log('Adding camera markers to map with', cameras.length, 'cameras');

                // Create individual markers for each camera
                cameras.forEach((camera, index) => {
                    try {
                        // Function to convert degrees to compass direction
                        function getCompassDirection(degrees) {
                            const directions = {
                                0: 'N', 45: 'NE', 90: 'E', 135: 'SE',
                                180: 'S', 225: 'SW', 270: 'W', 315: 'NW'
                            };
                            
                            // Normalize degrees
                            degrees = degrees % 360;
                            if (degrees < 0) degrees += 360;
                            
                            // Find closest direction
                            let closest = 0;
                            let minDiff = Math.abs(degrees - 0);
                            
                            for (const deg in directions) {
                                const diff = Math.abs(degrees - parseFloat(deg));
                                if (diff < minDiff) {
                                    minDiff = diff;
                                    closest = parseFloat(deg);
                                }
                            }
                            
                            return directions[closest];
                        }

                        // Function to calculate direction indicator position
                        function calculateDirectionPosition(degrees) {
                            // Normalize degrees
                            degrees = degrees % 360;
                            if (degrees < 0) degrees += 360;
                            
                            // Convert to radians (0° = North = top)
                            const radians = degrees * (Math.PI / 180);
                            
                            // Calculate position around a circle with radius 18px
                            const radius = 18;
                            const x = radius * Math.sin(radians);
                            const y = -radius * Math.cos(radians); // Negative because CSS y increases downward
                            
                            return { x, y };
                        }

                        // Create marker element with Font Awesome camera icon and direction indicator
                        const markerElement = document.createElement('div');
                        markerElement.className = 'camera-marker';
                        
                        const compassDirection = getCompassDirection(camera.directionDegrees);
                        const directionPos = calculateDirectionPosition(camera.directionDegrees);
                        
                        markerElement.innerHTML = `
                            <div class="camera-marker-inner ${camera.isActive ? 'active' : 'inactive'}">
                                <i class="fas fa-camera"></i>
                            </div>
                            <div class="camera-direction-indicator" style="left: ${15 + directionPos.x}px; top: ${15 + directionPos.y}px;">
                                ${compassDirection}
                            </div>
                        `;
                        
                        // Add CSS styles for the marker
                        const style = `
                            .camera-marker {
                                cursor: pointer;
                                width: 30px;
                                height: 30px;
                                position: relative;
                            }
                            .camera-marker-inner {
                                width: 30px;
                                height: 30px;
                                border-radius: 50%;
                                display: flex;
                                align-items: center;
                                justify-content: center;
                                border: 3px solid #ffffff;
                                box-shadow: 0 2px 8px rgba(0,0,0,0.3);
                                font-size: 14px;
                                color: #ffffff;
                                transition: transform 0.2s ease;
                            }
                            .camera-marker-inner:hover {
                                transform: scale(1.1);
                            }
                            .camera-marker-inner.active {
                                background-color: #FF6B35;
                            }
                            .camera-marker-inner.inactive {
                                background-color: #999999;
                            }
                            .camera-direction-indicator {
                                position: absolute;
                                background-color: #2c3e50;
                                color: white;
                                border: 2px solid #ffffff;
                                border-radius: 50%;
                                width: 20px;
                                height: 20px;
                                display: flex;
                                align-items: center;
                                justify-content: center;
                                font-size: 10px;
                                font-weight: bold;
                                box-shadow: 0 1px 4px rgba(0,0,0,0.3);
                                z-index: 1;
                                transform: translate(-50%, -50%);
                            }
                        `;

                        // Add styles if not already added
                        if (!document.getElementById('camera-marker-styles')) {
                            const styleSheet = document.createElement('style');
                            styleSheet.id = 'camera-marker-styles';
                            styleSheet.type = 'text/css';
                            styleSheet.innerText = style;
                            document.head.appendChild(styleSheet);
                        }

                        // Create marker
                        const marker = new mapboxgl.Marker({
                            element: markerElement,
                            anchor: 'center'
                        })
                        .setLngLat([camera.longitude, camera.latitude])
                        .addTo(m);

                        // Add click event to marker
                        markerElement.addEventListener('click', (e) => {
                            e.stopPropagation();
                            showCameraPopup({
                                properties: {
                                    id: camera.id,
                                    name: camera.name,
                                    isActive: camera.isActive,
                                    photoCount: camera.photoCount,
                                    brandModel: camera.brand + (camera.model ? ` / ${camera.model}` : ''),
                                    directionDegrees: camera.directionDegrees,
                                    directionText: compassDirection
                                }
                            }, { lng: camera.longitude, lat: camera.latitude });
                        });

                        // Store marker reference for cleanup
                        window.App._cameraMarkers.push(marker);

                        console.log(`Added camera marker ${index + 1}: ${camera.name}`);

                    } catch (error) {
                        console.error(`Error creating marker for camera ${camera.name}:`, error);
                    }
                });

                console.log('All camera markers added successfully');

                // Update map bounds to include cameras and property
                updateMapBoundsWithCameras(cameras);

            })
            .catch(error => {
                console.error('Error loading cameras:', error);
            });
    }

    function updateMapBoundsWithCameras(cameras) {
        const m = map();
        if (!m) return;

        const propertyCoords = window.App?.propertyCoords;
        if (!propertyCoords) return;

        // Create bounds that include property and cameras
        let bounds = new mapboxgl.LngLatBounds();
        bounds.extend([propertyCoords.lng, propertyCoords.lat]);

        // Add camera locations to bounds
        cameras.forEach(camera => {
            bounds.extend([camera.longitude, camera.latitude]);
        });

        // Check if we have more than just the property location
        const boundsArray = bounds.toArray();
        const hasMultiplePoints = (boundsArray[0][0] !== boundsArray[1][0]) || (boundsArray[0][1] !== boundsArray[1][1]);
        
        if (hasMultiplePoints) {
            // Fit to bounds with cameras and property
            m.fitBounds(bounds, {
                padding: {
                    top: 50,
                    bottom: 50, 
                    left: 50,
                    right: 50
                },
                maxZoom: 18, // More zoomed in for better view
                duration: 1200
            });
        } else {
            // If only property location, use a reasonable zoom level
            m.flyTo({
                center: [propertyCoords.lng, propertyCoords.lat],
                zoom: 16,
                duration: 1200
            });
        }
    }

    function showCameraPopup(camera, lngLat) {
        const properties = camera.properties;
        
        // Load camera details in sidebar instead of showing a modal
        if (window.App && window.App.loadSidebar) {
            window.App.loadSidebar(`/cameras/${properties.id}/details`, { push: true });
        } else {
            console.error('Sidebar loading not available');
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
                                    <label for="featureName" class="form-label">Feature Name (optional)</label>
                                    <input type="text" class="form-control" id="featureName" placeholder="Enter a custom name for this feature (e.g., 'SE Corner Bean Field')" maxlength="100">
                                    <small class="text-muted">Leave blank to use the default feature type name</small>
                                </div>
                                <div class="mb-3">
                                    <label for="featureType" class="form-label">Feature Type</label>
                                    <select class="form-select" id="featureType" required>
                                        ${window.FeatureUtils ? window.FeatureUtils.generateFeatureOptionsHtml() : '<option value="99">Other</option>'}
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

        const featureName = document.getElementById('featureName').value.trim() || null;
        const featureType = parseInt(document.getElementById('featureType').value);
        const notes = document.getElementById('featureNotes').value.trim() || null;

        // Convert GeoJSON geometry to WKT
        const geometryWkt = geometryToWKT(feature.geometry);

        const data = {
            classificationType: featureType,
            geometryWkt: geometryWkt,
            name: featureName,
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
            window.App.showModal('Error', 'Error saving feature. Please try again.', 'error');
        });
    };

    function showFeaturePopup(feature, lngLat) {
        const props = feature.properties;
        
        // Load feature details in sidebar instead of showing a modal
        if (window.App && window.App.loadSidebar) {
            window.App.loadSidebar(`/features/${props.id}/details`, { push: true });
        } else {
            console.error('Sidebar loading not available');
        }
    }

    // Function to close feature panel
    window.App.closeFeaturePopup = function() {
        if (window.App._currentFeatureModal) {
            window.App._currentFeatureModal.hide();
            window.App._currentFeatureModal = null;
        }
        
        // Also handle legacy popup cleanup if any exists
        if (window.App._currentFeaturePopup) {
            window.App._currentFeaturePopup.remove();
            window.App._currentFeaturePopup = null;
        }
        
        // Clean up any existing modals in DOM
        const existingModal = document.getElementById('featureDetailsModal');
        if (existingModal) {
            existingModal.remove();
        }
        
        // Clean up new panel
        if (window.App._currentFeaturePanel) {
            window.App._currentFeaturePanel.remove();
            window.App._currentFeaturePanel = null;
        }
        
        const existingPanel = document.getElementById('featureDetailsPanel');
        if (existingPanel) {
            existingPanel.remove();
        }
    };

    // Function to close camera modal/panel
    window.App.closeCameraModal = function() {
        if (window.App._currentCameraModal) {
            window.App._currentCameraModal.hide();
            window.App._currentCameraModal = null;
        }
        
        // Clean up any existing camera modals in DOM
        const existingModal = document.getElementById('cameraDetailsModal');
        if (existingModal) {
            existingModal.remove();
        }
        
        // Clean up new panel
        if (window.App._currentCameraPanel) {
            window.App._currentCameraPanel.remove();
            window.App._currentCameraPanel = null;
        }
        
        const existingPanel = document.getElementById('cameraDetailsPanel');
        if (existingPanel) {
            existingPanel.remove();
        }
    };

    // Function to pan to camera location (similar to focusPropertyFeature)
    window.App.panToCameraLocation = function(lng, lat) {
        console.log('Panning to camera location:', lng, lat);
        
        const m = map();
        if (!m) {
            console.error('Map not available for panning to camera');
            return;
        }
        
        if (!Number.isFinite(lng) || !Number.isFinite(lat)) {
            console.error('Invalid camera coordinates:', lng, lat);
            return;
        }
        
        // Pan to camera location with appropriate zoom
        m.flyTo({
            center: [lng, lat],
            zoom: Math.max(m.getZoom(), 17), // Zoom in closer for camera location
            duration: 1500
        });
        
        console.log('Panning to camera complete');
    };

    window.App.editPropertyFeature = function(featureId) {
        console.log('=== FEATURE EDIT DEBUG START ===');
        console.log('window.App.editPropertyFeature called with featureId:', featureId);
        console.log('window.App exists:', !!(window.App));
        console.log('window.App.loadSidebar exists:', !!(window.App && window.App.loadSidebar));
        console.log('window.App.loadSidebar type:', typeof (window.App && window.App.loadSidebar));
        
        // Close any existing popup or modal when entering edit mode
        if (window.App.closeFeaturePopup) {
            console.log('Closing any existing feature popup');
            window.App.closeFeaturePopup();
        } else {
            console.log('window.App.closeFeaturePopup not available');
        }
        
        // Prevent any default behavior or event propagation
        if (event) {
            console.log('Preventing event default and propagation');
            event.preventDefault();
            event.stopPropagation();
        }
        
        // Load feature edit form in sidebar instead of floating panel
        if (window.App && typeof window.App.loadSidebar === 'function') {
            const editUrl = `/features/${featureId}/edit`;
            console.log('Loading sidebar for feature edit:', editUrl);
            
            try {
                const loadPromise = window.App.loadSidebar(editUrl, { push: true });
                console.log('loadSidebar call completed, promise:', loadPromise);
                
                if (loadPromise && typeof loadPromise.then === 'function') {
                    loadPromise.then(() => {
                        console.log('Sidebar loaded successfully for feature edit');
                        console.log('=== FEATURE EDIT DEBUG SUCCESS ===');
                    }).catch((error) => {
                        console.error('Error loading sidebar for feature edit:', error);
                        console.log('=== FEATURE EDIT DEBUG ERROR ===');
                        // Show error to user
                        if (window.App.showModal) {
                            window.App.showModal('Error', 'Failed to load feature editor. Please try again.', 'error');
                        }
                    });
                } else {
                    console.log('Sidebar loadSidebar function does not return a promise');
                    console.log('=== FEATURE EDIT DEBUG NO PROMISE ===');
                }
            } catch (error) {
                console.error('Exception calling loadSidebar:', error);
                console.log('=== FEATURE EDIT DEBUG EXCEPTION ===');
                // Fallback to regular navigation
                console.log('Using fallback navigation due to exception');
                window.location.href = editUrl;
            }
        } else {
            console.error('Sidebar loading not available - loadSidebar function missing');
            console.log('Available App functions:', Object.keys(window.App || {}));
            
            // Fallback to regular navigation
            const editUrl = `/features/${featureId}/edit`;
            console.log('Using fallback navigation to:', editUrl);
            console.log('=== FEATURE EDIT DEBUG FALLBACK ===');
            window.location.href = editUrl;
        }
    };

    function enableGeometryEditing(feature) {
        const draw = window.App._draw;
        if (!draw) {
            console.warn('Drawing control not available');
            return;
        }

        console.log('Enabling geometry editing for feature');
        
        // Convert the feature to a format that MapboxDraw can understand
        const drawFeature = {
            type: 'Feature',
            properties: {},
            geometry: feature.geometry
        };
        
        try {
            // Add the feature to the draw control
            const featureIds = draw.add(drawFeature);
            console.log('Added feature to draw control:', featureIds);
            
            // Store the draw feature ID for later cleanup
            window.App._editingDrawFeatureId = featureIds[0];
            
            // Put draw control in direct select mode for this feature
            draw.changeMode('direct_select', { featureId: featureIds[0] });
            
        } catch (error) {
            console.error('Error enabling geometry editing:', error);
            window.App.showModal("Error", 'Failed to enable geometry editing. Please try again.', "error");
        }
    }

    function disableGeometryEditing() {
        const draw = window.App._draw;
        if (draw && window.App._editingDrawFeatureId) {
            try {
                // Remove the feature from draw control
                draw.delete(window.App._editingDrawFeatureId);
                draw.changeMode('simple_select');
                console.log('Disabled geometry editing');
            } catch (error) {
                console.warn('Error disabling geometry editing:', error);
            }
            
            window.App._editingDrawFeatureId = null;
        }
    }

    window.App.cancelFeatureEdit = function() {
        // Clean up editing state
        disableGeometryEditing();
        window.App._editingFeature = null;
        window.App._editingFeatureId = null;
        
        console.log('Feature editing cancelled');
        
        // Go back in history to return to previous sidebar view
        if (window.history.length > 1) {
            window.history.back();
        } else {
            // If no history, close sidebar
            if (window.App && window.App.loadSidebar) {
                // Load empty or default sidebar content
                const sidebar = document.getElementById('sidebar');
                if (sidebar && sidebar.classList.contains('show')) {
                    sidebar.classList.remove('show');
                }
            }
        }
    };

    window.App.saveFeatureEdit = function(featureId) {
        const feature = window.App._editingFeature;
        if (!feature) {
            console.error('No feature being edited');
            return;
        }

        const featureName = document.getElementById('editFeatureName').value.trim() || null;
        const featureType = parseInt(document.getElementById('editFeatureType').value);
        const notes = document.getElementById('editFeatureNotes').value.trim() || null;
        let geometryWkt = geometryToWKT(feature.geometry); // Default to original geometry
        
        // Check if geometry was being edited
        const draw = window.App._draw;
        if (window.App._editingDrawFeatureId && draw) {
            try {
                // Get the modified geometry from the draw control
                const drawFeature = draw.get(window.App._editingDrawFeatureId);
                if (drawFeature) {
                    geometryWkt = geometryToWKT(drawFeature.geometry);
                    console.log('Using modified geometry:', geometryWkt);
                }
            } catch (error) {
                console.warn('Could not get modified geometry, using original:', error);
            }
        }

        const data = {
            classificationType: featureType,
            geometryWkt: geometryWkt,
            name: featureName,
            notes: notes
        };

        const url = `/features/${featureId}`;
        const method = 'PUT';

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
                // Clean up editing state
                disableGeometryEditing();
                window.App._editingFeature = null;
                window.App._editingFeatureId = null;

                // Reload property details view to refresh features list
                const propertyId = window.App._currentPropertyId;
                if (propertyId) {
                    refreshPropertyDetailsView(propertyId);
                }

                console.log('Feature updated successfully');

                // Go back to feature details view in sidebar
                if (window.App && window.App.loadSidebar) {
                    window.App.loadSidebar(`/features/${featureId}/details`, { push: true });
                } else {
                    // Fallback: go back in history
                    if (window.history.length > 1) {
                        window.history.back();
                    }
                }
            } else {
                throw new Error('Failed to update feature');
            }
        })
        .catch(error => {
            console.error('Error updating feature:', error);
            window.App.showModal("Error", 'Error updating feature. Please try again.', "error");
        });
    };

    // Implement the focus feature functionality (the "eyeball" button)
    window.App.focusPropertyFeature = function(featureId) {
        console.log('Focusing on feature:', featureId);
        
        const m = map();
        if (!m) {
            console.error('Map not available for focusing feature');
            return;
        }
        
        // Get the features source
        const source = m.getSource('property-features');
        if (!source) {
            console.error('Property features source not found on map');
            window.App.showModal("Error", 'Features not loaded on map. Please refresh and try again.', "error");
            return;
        }
        
        // Get the feature data
        const sourceData = source._data;
        if (!sourceData || !sourceData.features) {
            console.error('No feature data available');
            window.App.showModal("Error", 'No feature data available. Please refresh and try again.', "error");
            return;
        }
        
        // Find the feature with the specified ID
        const targetFeature = sourceData.features.find(f => f.properties.id === featureId);
        if (!targetFeature) {
            console.error('Feature not found:', featureId);
            window.App.showModal("Error", 'Feature not found on map. Please refresh and try again.', "error");
            return;
        }
        
        console.log('Found target feature:', targetFeature);
        
        // Calculate bounds for the feature
        let bounds = new mapboxgl.LngLatBounds();
        
        if (targetFeature.geometry.type === 'Point') {
            const coords = targetFeature.geometry.coordinates;
            bounds.extend(coords);
            // For points, create a small buffer around the point
            const buffer = 0.001; // Approximately 100 meters
            bounds.extend([coords[0] - buffer, coords[1] - buffer]);
            bounds.extend([coords[0] + buffer, coords[1] + buffer]);
        } else if (targetFeature.geometry.type === 'LineString') {
            targetFeature.geometry.coordinates.forEach(coord => bounds.extend(coord));
        } else if (targetFeature.geometry.type === 'Polygon') {
            targetFeature.geometry.coordinates[0].forEach(coord => bounds.extend(coord));
        }
        
        // Fit map to feature bounds
        m.fitBounds(bounds, {
            padding: 50,
            maxZoom: 18,
            duration: 1500
        });
        
        // Optional: Temporarily highlight the feature
        highlightFeature(targetFeature, featureId);
    };

    window.App.deletePropertyFeature = function(featureId) {
        window.App.showConfirmModal('Delete Feature', 'Are you sure you want to delete this feature?', function() {
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
                window.App.showModal("Error", 'Error deleting feature. Please try again.', "error");
            });
        });
    };

    // Function to refresh the property details view after feature changes
    function refreshPropertyDetailsView(propertyId) {
        console.log('Refreshing property details view for property:', propertyId);
        
        if (!propertyId) {
            console.error('Property ID is required for refreshing property details');
            return;
        }
        
        // Use the sidebar loader to refresh the property details view
        if (window.App && window.App.loadSidebar) {
            const propertyDetailsUrl = `/properties/${propertyId}/details`;
            console.log('Loading sidebar with URL:', propertyDetailsUrl);
            
            // Show a loading indicator
            showRefreshIndicator(true);
            
            window.App.loadSidebar(propertyDetailsUrl, { push: false }).then(() => {
                console.log('Property details view refreshed successfully');
                showRefreshIndicator(false);
                
                // After the view is reloaded, expand the features accordion to show the changes
                setTimeout(() => {
                    const featuresAccordion = document.getElementById('featuresCollapse');
                    if (featuresAccordion && !featuresAccordion.classList.contains('show')) {
                        console.log('Expanding features accordion');
                        // Use Bootstrap's collapse API to show the features section
                        try {
                            const bsCollapse = new bootstrap.Collapse(featuresAccordion, {
                                show: true
                            });
                        } catch (e) {
                            console.warn('Could not expand features accordion:', e);
                        }
                    }
                    
                    // Re-initialize property features for the refreshed view
                    if (window.App && window.App.initializePropertyFeatures) {
                        console.log('Re-initializing property features after refresh');
                        window.App.initializePropertyFeatures(propertyId);
                    }
                }, 300); // Increased delay to ensure DOM is fully updated
            }).catch(error => {
                console.error('Error refreshing property details view:', error);
                showRefreshIndicator(false);
                
                // Show user-friendly error message
                showFeatureError('Failed to refresh property details: ' + error.message);
                
                // Fallback: just reload features if sidebar loader fails
                console.log('Falling back to reloading features only');
                setTimeout(() => {
                    loadPropertyFeatures(propertyId);
                }, 1000);
            });
        } else {
            console.warn('Sidebar loader not available, falling back to reloading features');
            // Fallback: just reload features if sidebar loader not available
            loadPropertyFeatures(propertyId);
        }
    }

    function showRefreshIndicator(show) {
        let indicator = document.getElementById('refresh-indicator');
        
        if (show) {
            if (!indicator) {
                indicator = document.createElement('div');
                indicator.id = 'refresh-indicator';
                indicator.className = 'alert alert-info position-fixed';
                indicator.style.cssText = 'top: 70px; left: 20px; z-index: 1000; max-width: 250px;';
                indicator.innerHTML = '<i class="fas fa-sync-alt fa-spin me-2"></i>Refreshing view...';
                document.body.appendChild(indicator);
            }
        } else {
            if (indicator) {
                indicator.remove();
            }
        }
    }

    // Helper functions
    function parseSimpleWKT(wkt) {
        // Enhanced WKT parsing with better error handling and debugging
        if (!wkt || typeof wkt !== 'string') {
            console.error('Invalid WKT input:', wkt);
            throw new Error('Invalid WKT: must be a non-empty string');
        }
        
        wkt = wkt.trim().toUpperCase();
        console.log('Parsing WKT:', wkt);
        
        try {
            if (wkt.startsWith('POINT')) {
                const match = wkt.match(/POINT\s*\(\s*([^)]+)\s*\)/);
                if (!match) {
                    console.error('POINT regex failed for:', wkt);
                    throw new Error('Invalid POINT WKT format');
                }
                
                const coordString = match[1].trim();
                console.log('POINT coordinate string:', coordString);
                
                const coords = coordString.split(/\s+/);
                if (coords.length < 2) {
                    console.error('POINT insufficient coordinates:', coords);
                    throw new Error('POINT must have at least 2 coordinates');
                }
                
                const lng = parseFloat(coords[0]);
                const lat = parseFloat(coords[1]);
                
                if (isNaN(lng) || isNaN(lat)) {
                    console.error('POINT invalid coordinate values:', coords);
                    throw new Error('POINT coordinates must be valid numbers');
                }
                
                console.log('Parsed POINT:', [lng, lat]);
                return {
                    type: 'Point',
                    coordinates: [lng, lat]
                };
                
            } else if (wkt.startsWith('LINESTRING')) {
                const match = wkt.match(/LINESTRING\s*\(\s*([^)]+)\s*\)/);
                if (!match) {
                    console.error('LINESTRING regex failed for:', wkt);
                    throw new Error('Invalid LINESTRING WKT format');
                }
                
                const coordString = match[1].trim();
                console.log('LINESTRING coordinate string:', coordString);
                
                const coordinates = coordString.split(',').map(pair => {
                    const coords = pair.trim().split(/\s+/);
                    if (coords.length < 2) {
                        console.error('LINESTRING invalid coordinate pair:', pair);
                        throw new Error('Each coordinate pair must have at least 2 values');
                    }
                    const lng = parseFloat(coords[0]);
                    const lat = parseFloat(coords[1]);
                    if (isNaN(lng) || isNaN(lat)) {
                        console.error('LINESTRING invalid coordinate values:', coords);
                        throw new Error('Coordinate values must be valid numbers');
                    }
                    return [lng, lat];
                });
                
                if (coordinates.length < 2) {
                    console.error('LINESTRING insufficient coordinate pairs:', coordinates.length);
                    throw new Error('LINESTRING must have at least 2 coordinate pairs');
                }
                
                console.log('Parsed LINESTRING:', coordinates);
                return {
                    type: 'LineString',
                    coordinates: coordinates
                };
                
            } else if (wkt.startsWith('POLYGON')) {
                // Improved regex to handle nested parentheses properly
                const match = wkt.match(/POLYGON\s*\(\s*\(([^)]+(?:\)[^)]*)*)\)\s*\)/);
                if (!match) {
                    console.error('POLYGON regex failed for:', wkt);
                    // Try simpler fallback regex
                    const fallbackMatch = wkt.match(/POLYGON\s*\(\s*\(([^)]+)\)\s*\)/);
                    if (!fallbackMatch) {
                        throw new Error('Invalid POLYGON WKT format');
                    }
                    console.log('Using fallback POLYGON parsing');
                    var coordString = fallbackMatch[1].trim();
                } else {
                    var coordString = match[1].trim();
                }
                
                console.log('POLYGON coordinate string:', coordString);
                
                const coordinates = coordString.split(',').map(pair => {
                    const coords = pair.trim().split(/\s+/);
                    if (coords.length < 2) {
                        console.error('POLYGON invalid coordinate pair:', pair);
                        throw new Error('Each coordinate pair must have at least 2 values');
                    }
                    const lng = parseFloat(coords[0]);
                    const lat = parseFloat(coords[1]);
                    if (isNaN(lng) || isNaN(lat)) {
                        console.error('POLYGON invalid coordinate values:', coords);
                        throw new Error('Coordinate values must be valid numbers');
                    }
                    return [lng, lat];
                });
                
                if (coordinates.length < 3) {
                    console.error('POLYGON insufficient coordinate pairs:', coordinates.length);
                    throw new Error('POLYGON must have at least 3 coordinate pairs');
                }
                
                // Ensure polygon is closed (first and last points are the same)
                const first = coordinates[0];
                const last = coordinates[coordinates.length - 1];
                if (first[0] !== last[0] || first[1] !== last[1]) {
                    console.log('Closing POLYGON by adding first point to end');
                    coordinates.push([first[0], first[1]]);
                }
                
                console.log('Parsed POLYGON:', coordinates);
                return {
                    type: 'Polygon',
                    coordinates: [coordinates]
                };
            }
            
            console.error('Unsupported WKT format:', wkt);
            throw new Error('Unsupported WKT format: ' + wkt.substring(0, 50) + '...');
            
        } catch (error) {
            console.error('Error parsing WKT:', wkt, error);
            throw error;
        }
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
        return window.FeatureUtils ? window.FeatureUtils.getFeatureName(classificationType) : 'Unknown';
    }

    function getFeatureColor(classificationType) {
        return window.FeatureUtils ? window.FeatureUtils.getFeatureColor(classificationType) : '#999999';
    }

    // Function to close sidebar if it's open
    function closeSidebarIfOpen() {
        const aside = document.getElementById('sidebar');
        if (aside && !aside.classList.contains('collapsed')) {
            console.log('Closing sidebar for feature editing');
            
            // Trigger the collapse similar to how the toggle button works
            aside.classList.add('collapsed');
            aside.style.transform = 'translateX(-100%)';
            
            // Update the toggle button icon if present
            const btn = document.getElementById('sidebar-toggle');
            if (btn) {
                const icon = btn.querySelector('i');
                if (icon) {
                    icon.className = 'fas fa-chevron-right';
                }
            }
            
            // Update toggle position after animation
            setTimeout(() => {
                if (window.updateTogglePosition) {
                    window.updateTogglePosition();
                }
            }, 300);
        }
    }

    // Function to temporarily highlight a feature
    function highlightFeature(targetFeature, featureId) {
        const m = map();
        if (!m) return;
        
        console.log('Highlighting feature:', featureId);
        
        // Remove any existing highlight
        if (m.getLayer('feature-highlight')) {
            m.removeLayer('feature-highlight');
        }
        if (m.getSource('feature-highlight')) {
            m.removeSource('feature-highlight');
        }
        
        // Create highlight source
        m.addSource('feature-highlight', {
            type: 'geojson',
            data: {
                type: 'FeatureCollection',
                features: [targetFeature]
            }
        });
        
        // Add highlight layer based on geometry type
        if (targetFeature.geometry.type === 'Point') {
            m.addLayer({
                id: 'feature-highlight',
                type: 'circle',
                source: 'feature-highlight',
                paint: {
                    'circle-color': '#ff0000',
                    'circle-radius': 12,
                    'circle-stroke-color': '#ffffff',
                    'circle-stroke-width': 3,
                    'circle-opacity': 0.8
                }
            });
        } else {
            m.addLayer({
                id: 'feature-highlight',
                type: 'line',
                source: 'feature-highlight',
                paint: {
                    'line-color': '#ff0000',
                    'line-width': 5,
                    'line-opacity': 0.8
                }
            });
        }
        
        // Remove highlight after 3 seconds
        setTimeout(() => {
            if (m.getLayer('feature-highlight')) {
                m.removeLayer('feature-highlight');
            }
            if (m.getSource('feature-highlight')) {
                m.removeSource('feature-highlight');
            }
            console.log('Feature highlight removed');
        }, 3000);
    }
    // Expose property features functions globally
    window.savePropertyFeature = function() {
        window.App.savePropertyFeature();
    };
    
    window.editPropertyFeature = function(featureId, event) {
        console.log('=== GLOBAL EDIT WRAPPER CALLED ===');
        console.log('Global editPropertyFeature called with:', featureId);
        console.log('Event:', event);
        
        // Set global event for debugging
        window._debugEvent = event;
        
        if (window.App && typeof window.App.editPropertyFeature === 'function') {
            console.log('Calling window.App.editPropertyFeature');
            window.App.editPropertyFeature(featureId);
        } else {
            console.error('window.App.editPropertyFeature not available');
            console.log('Available App functions:', Object.keys(window.App || {}));
            // Emergency fallback
            window.location.href = `/features/${featureId}/edit`;
        }
    };
    
    window.deletePropertyFeature = function(featureId) {
        window.App.deletePropertyFeature(featureId);
    };
    
    window.focusPropertyFeature = function(featureId) {
        window.App.focusPropertyFeature(featureId);
    };
    
    window.saveFeatureEdit = function(featureId) {
        window.App.saveFeatureEdit(featureId);
    };
    
    window.cancelFeatureEdit = function() {
        window.App.cancelFeatureEdit();
    };
    
    window.closeFeaturePopup = function() {
        window.App.closeFeaturePopup();
    };

    window.closeFeaturePopup = function() {
        window.App.closeFeaturePopup();
    };

    window.closeCameraModal = function() {
        window.App.closeCameraModal();
    };
    
    window.panToCameraLocation = function(lng, lat) {
        window.App.panToCameraLocation(lng, lat);
    };
    
    window.disableGeometryEditing = function() {
        if (window.App._editingDrawFeatureId) {
            const draw = window.App._draw;
            if (draw) {
                try {
                    draw.delete(window.App._editingDrawFeatureId);
                    draw.changeMode('simple_select');
                } catch (error) {
                    console.warn('Error disabling geometry editing:', error);
                }
                window.App._editingDrawFeatureId = null;
            }
        }
        
        const instructionDiv = document.getElementById('editing-instructions');
        if (instructionDiv) {
            instructionDiv.remove();
        }
        
        const editGeometryCheckbox = document.getElementById('editGeometry');
        if (editGeometryCheckbox) {
            editGeometryCheckbox.checked = false;
        }
    };

    // Modal helper to replace JavaScript alerts
    function showModal(title, message, type = 'info') {
        // Remove any existing modal
        const existingModal = document.getElementById('app-modal');
        if (existingModal) {
            existingModal.remove();
        }

        const iconClass = type === 'error' ? 'fas fa-exclamation-triangle text-danger' :
                         type === 'success' ? 'fas fa-check-circle text-success' :
                         'fas fa-info-circle text-info';

        const modalHtml = `
            <div class="modal fade" id="app-modal" tabindex="-1" aria-labelledby="appModalLabel" aria-hidden="true">
                <div class="modal-dialog">
                    <div class="modal-content">
                        <div class="modal-header">
                            <h5 class="modal-title" id="appModalLabel">
                                <i class="${iconClass} me-2"></i>${title}
                            </h5>
                            <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                        </div>
                        <div class="modal-body">
                            ${message}
                        </div>
                        <div class="modal-footer">
                            <button type="button" class="btn btn-primary" data-bs-dismiss="modal">OK</button>
                        </div>
                    </div>
                </div>
            </div>
        `;

        document.body.insertAdjacentHTML('beforeend', modalHtml);
        const modalElement = document.getElementById('app-modal');
        const modal = new bootstrap.Modal(modalElement);
        modal.show();

        // Clean up after modal is hidden
        modalElement.addEventListener('hidden.bs.modal', () => {
            modalElement.remove();
        });
    }

    function showConfirmModal(title, message, onConfirm) {
        // Remove any existing modal
        const existingModal = document.getElementById('app-confirm-modal');
        if (existingModal) {
            existingModal.remove();
        }

        const modalHtml = `
            <div class="modal fade" id="app-confirm-modal" tabindex="-1" aria-labelledby="appConfirmModalLabel" aria-hidden="true">
                <div class="modal-dialog">
                    <div class="modal-content">
                        <div class="modal-header">
                            <h5 class="modal-title" id="appConfirmModalLabel">
                                <i class="fas fa-question-circle text-warning me-2"></i>${title}
                            </h5>
                            <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                        </div>
                        <div class="modal-body">
                            ${message}
                        </div>
                        <div class="modal-footer">
                            <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
                            <button type="button" class="btn btn-danger" id="confirm-action">Confirm</button>
                        </div>
                    </div>
                </div>
            </div>
        `;

        document.body.insertAdjacentHTML('beforeend', modalHtml);
        const modalElement = document.getElementById('app-confirm-modal');
        const modal = new bootstrap.Modal(modalElement);
        
        // Handle confirm button click
        document.getElementById('confirm-action').addEventListener('click', () => {
            modal.hide();
            if (onConfirm && typeof onConfirm === 'function') {
                onConfirm();
            }
        });
        
        modal.show();

        // Clean up after modal is hidden
        modalElement.addEventListener('hidden.bs.modal', () => {
            modalElement.remove();
        });
    }

    // Expose modal functions globally
    window.App.showModal = showModal;
    window.App.showConfirmModal = showConfirmModal;

    // Expose wireCameraForm function
    window.App.wireCameraForm = wireCameraForm;

    // Function to edit camera using sidebar loading
    window.App.editCamera = function(propertyId, cameraId) {
        if (window.App && window.App.loadSidebar) {
            window.App.loadSidebar(`/properties/${propertyId}/cameras/${cameraId}/edit`, { push: true });
        } else {
            // Fallback to regular navigation
            window.location.href = `/properties/${propertyId}/cameras/${cameraId}/edit`;
        }
    };

    // Camera-related global functions
    
    // Robust editCamera function that will always be available
    window.handleEditCamera = function(propertyId, cameraId) {
        console.log('Edit camera clicked:', propertyId, cameraId);
        
        // Try to use the App.editCamera function first
        if (window.App && typeof window.App.editCamera === 'function') {
            window.App.editCamera(propertyId, cameraId);
            return;
        }
        
        // Fallback: use sidebar loading directly
        if (window.App && typeof window.App.loadSidebar === 'function') {
            window.App.loadSidebar(`/properties/${propertyId}/cameras/${cameraId}/edit`, { push: true });
            return;
        }
        
        // Ultimate fallback: regular navigation
        console.warn('No sidebar loading available, using regular navigation');
        window.location.href = `/properties/${propertyId}/cameras/${cameraId}/edit`;
    };

    // Function to cancel camera edit and return to camera details
    window.cancelCameraEdit = function(cameraId) {
        console.log('Cancel camera edit clicked:', cameraId);
        
        if (window.App && window.App.loadSidebar) {
            window.App.loadSidebar(`/cameras/${cameraId}/details`, { push: true });
        } else {
            // Fallback to regular navigation
            window.location.href = `/cameras/${cameraId}/details`;
        }
    };

    // Pan to camera location function
    window.panToCameraLocation = function(lng, lat) {
        if (window.App && window.App.panToCameraLocation) {
            window.App.panToCameraLocation(lng, lat);
        } else {
            console.log('Pan to camera location:', lng, lat);
        }
    };

    // Expose editCamera function globally for backwards compatibility
    window.editCamera = function(propertyId, cameraId) {
        window.handleEditCamera(propertyId, cameraId);
    };

    // Upload-related global functions
    
    // Function to handle upload photos using sidebar loading
    window.handleUploadPhotos = function(cameraId) {
        console.log('Upload photos clicked:', cameraId);
        
        if (window.App && window.App.loadSidebar) {
            window.App.loadSidebar(`/cameras/${cameraId}/upload`, { push: true });
        } else {
            // Fallback to regular navigation
            window.location.href = `/cameras/${cameraId}/upload`;
        }
    };

    // Function to reset upload button state
    window.resetUploadButton = function() {
        const uploadBtn = document.getElementById('uploadBtn');
        const uploadSpinner = document.getElementById('uploadSpinner');
        const uploadText = document.getElementById('uploadText');
        
        if (uploadBtn) uploadBtn.disabled = false;
        if (uploadSpinner) uploadSpinner.classList.add('d-none');
        if (uploadText) uploadText.textContent = 'Upload';
    };

    // Function to handle upload form submission with proper sidebar integration
    window.handleUploadFormSubmission = function(form) {
        const formData = new FormData(form);
        const uploadBtn = document.getElementById('uploadBtn');
        const uploadSpinner = document.getElementById('uploadSpinner');
        const uploadText = document.getElementById('uploadText');
        const errorContainer = document.getElementById('errorContainer');
        
        // Show loading state
        if (uploadBtn) uploadBtn.disabled = true;
        if (uploadSpinner) uploadSpinner.classList.remove('d-none');
        if (uploadText) uploadText.textContent = 'Uploading...';
        if (errorContainer) errorContainer.classList.add('d-none');
        
        return fetch(form.action, {
            method: 'POST',
            body: formData,
            headers: {
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
            }
        })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                // Show success modal
                const message = data.photoCount === 1 
                    ? `${data.photoCount} photo uploaded successfully!`
                    : `${data.photoCount} photos uploaded successfully!`;
                
                if (window.App && window.App.showModal) {
                    window.App.showModal('Upload Successful', message, 'success');
                } else {
                    alert(message);
                }
                
                // Navigate back to camera photos view
                if (window.App && window.App.loadSidebar) {
                    window.App.loadSidebar(`/cameras/${data.cameraId}/photos`, { push: true });
                } else {
                    window.location.href = `/cameras/${data.cameraId}/photos`;
                }
            } else {
                // Show error
                const errorMessage = data.error || 'Upload failed. Please try again.';
                if (errorContainer) {
                    const errorMessageElement = document.getElementById('errorMessage');
                    if (errorMessageElement) errorMessageElement.textContent = errorMessage;
                    errorContainer.classList.remove('d-none');
                } else if (window.App && window.App.showModal) {
                    window.App.showModal('Upload Error', errorMessage, 'error');
                } else {
                    alert('Error: ' + errorMessage);
                }
                window.resetUploadButton();
            }
        })
        .catch(error => {
            console.error('Upload error:', error);
            const errorMessage = 'Upload failed. Please try again.';
            if (errorContainer) {
                const errorMessageElement = document.getElementById('errorMessage');
                if (errorMessageElement) errorMessageElement.textContent = errorMessage;
                errorContainer.classList.remove('d-none');
            } else if (window.App && window.App.showModal) {
                window.App.showModal('Upload Error', errorMessage, 'error');
            } else {
                alert('Error: ' + errorMessage);
            }
            window.resetUploadButton();
        });
    };

    // Export geometry editing functions globally for use in sidebar views
    window.enableGeometryEditing = enableGeometryEditing;
    window.disableGeometryEditing = disableGeometryEditing;
})();