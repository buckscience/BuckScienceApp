// BuckLens Analytics Chart Components
window.BuckLens = window.BuckLens || {};

BuckLens.Charts = {
    // Store chart instances for cleanup
    chartInstances: {},
    
    // Flag to prevent multiple initializations
    initialized: false,

    // Color palette for consistent theming
    colors: {
        primary: '#527A52',
        secondary: '#8CAF8C',
        accent: '#4a5a5f',
        danger: '#dc3545',
        warning: '#ffc107',
        info: '#17a2b8',
        light: '#f8f9fa',
        dark: '#343a40'
    },

    // Moon phase icons mapping using Weather Icons
    moonPhaseIcons: {
        'New Moon': '<i class="wi wi-moon-new"></i>',
        'Waxing Crescent': '<i class="wi wi-moon-waxing-crescent-1"></i>',
        'First Quarter': '<i class="wi wi-moon-first-quarter"></i>',
        'Waxing Gibbous': '<i class="wi wi-moon-waxing-gibbous-1"></i>',
        'Full Moon': '<i class="wi wi-moon-full"></i>',
        'Waning Gibbous': '<i class="wi wi-moon-waning-gibbous-1"></i>',
        'Last Quarter': '<i class="wi wi-moon-third-quarter"></i>',
        'Waning Crescent': '<i class="wi wi-moon-waning-crescent-1"></i>'
    },

    // Wind direction icons
    windDirectionIcons: {
        'N': '↑', 'NNE': '↗', 'NE': '↗', 'ENE': '↗',
        'E': '→', 'ESE': '↘', 'SE': '↘', 'SSE': '↘',
        'S': '↓', 'SSW': '↙', 'SW': '↙', 'WSW': '↙',
        'W': '←', 'WNW': '↖', 'NW': '↖', 'NNW': '↖'
    },

    // Chart color schemes - all green shades for consistent theming
    colorSchemes: {
        default: ['#527A52', '#8CAF8C', '#6B8E6B', '#9CC29C', '#4E734E', '#B8D6B8', '#3E5A3E', '#7A9A7A'],
        greenShades: ['#2E4A2E', '#3E5A3E', '#4E734E', '#527A52', '#5E855E', '#6B8E6B', '#7A9A7A', '#8CAF8C', '#9CC29C', '#AECFAE', '#B8D6B8', '#C4E0C4', '#D4E6D4', '#E4F0E4'],
        timeOfDay: {
            'Morning': '#6B8E6B',
            'Midday': '#4E734E',
            'Evening': '#7A9A7A',
            'Night': '#3E5A3E'
        },
        windDirection: {
            'N': '#527A52', 'NE': '#6B8E6B', 'E': '#8CAF8C', 'SE': '#9CC29C',
            'S': '#4E734E', 'SW': '#7A9A7A', 'W': '#B8D6B8', 'NW': '#3E5A3E'
        }
    },

    // Initialize all charts for a profile
    async init(profileId) {
        try {
            // Ensure canvas elements exist before starting
            this.ensureCanvasElements();
            
            // Show loading state in summary only
            this.showLoading();

            // Wait a small amount for the accordion to fully expand
            await new Promise(resolve => setTimeout(resolve, 100));

            // Load summary data first
            await this.loadSummary(profileId);

            // Load all charts
            await Promise.all([
                this.loadCameraChart(profileId),
                this.loadTimeOfDayChart(profileId),
                this.loadMoonPhaseChart(profileId),
                this.loadWindDirectionChart(profileId),
                this.loadTemperatureChart(profileId),
                this.loadSightingHeatmap(profileId)
            ]);

            // Hide loading state
            this.hideLoading();
        } catch (error) {
            console.error('Error initializing BuckLens charts:', error);
            this.showError('Failed to load analytics data. Please try again.');
        }
    },

    // Ensure all required canvas elements exist in the DOM
    ensureCanvasElements() {
        const chartsContainer = document.getElementById('bucklensCharts');
        if (!chartsContainer) {
            console.error('Charts container not found');
            return;
        }

        // Check if canvas elements already exist
        const requiredCanvases = ['cameraChart', 'timeOfDayChart', 'moonPhaseChart', 'windDirectionChart', 'temperatureChart'];
        const missingCanvases = requiredCanvases.filter(id => !document.getElementById(id));
        
        if (missingCanvases.length === 0) {
            return; // All canvas elements exist
        }

        // If canvas elements are missing, create the full chart structure
        this.createChartsHTML();
    },

    // Create the complete charts HTML structure
    createChartsHTML() {
        const container = document.getElementById('bucklensCharts');
        if (!container) return;

        container.innerHTML = `
            <div class="row g-4">
                <!-- Sightings by Camera -->
                <div class="col-md-6">
                    <div class="card border-0 shadow-sm bucklens-chart-card">
                        <div class="card-body">
                            <div class="bucklens-chart-container">
                                <canvas id="cameraChart"></canvas>
                            </div>
                        </div>
                    </div>
                </div>

                <!-- Sightings by Time of Day -->
                <div class="col-md-6">
                    <div class="card border-0 shadow-sm bucklens-chart-card">
                        <div class="card-body">
                            <div class="bucklens-chart-container">
                                <canvas id="timeOfDayChart"></canvas>
                            </div>
                        </div>
                    </div>
                </div>

                <!-- Sightings by Moon Phase -->
                <div class="col-md-6">
                    <div class="card border-0 shadow-sm bucklens-chart-card">
                        <div class="card-body">
                            <div class="bucklens-chart-container">
                                <canvas id="moonPhaseChart"></canvas>
                            </div>
                        </div>
                    </div>
                </div>

                <!-- Sightings by Wind Direction -->
                <div class="col-md-6">
                    <div class="card border-0 shadow-sm bucklens-chart-card">
                        <div class="card-body">
                            <div class="bucklens-chart-container">
                                <canvas id="windDirectionChart"></canvas>
                            </div>
                        </div>
                    </div>
                </div>

                <!-- Sightings by Temperature -->
                <div class="col-md-6">
                    <div class="card border-0 shadow-sm bucklens-chart-card">
                        <div class="card-body">
                            <div class="bucklens-chart-container">
                                <canvas id="temperatureChart"></canvas>
                            </div>
                        </div>
                    </div>
                </div>

                <!-- Sighting Heatmap/Locations -->
                <div class="col-md-6">
                    <div class="card border-0 shadow-sm bucklens-chart-card">
                        <div class="card-header bg-white border-0">
                            <h6 class="mb-0"><i class="fas fa-map-marked-alt me-2"></i>Sighting Locations</h6>
                        </div>
                        <div class="card-body">
                            <div id="sightingHeatmap" style="min-height: 250px;">
                                <!-- Heatmap/location data will be populated by JavaScript -->
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        `;
    },

    // Load and display summary data
    async loadSummary(profileId) {
        const response = await fetch(`/profiles/${profileId}/analytics/summary`);
        if (!response.ok) throw new Error('Failed to load summary');
        
        const data = await response.json();
        this.updateSummary(data);
    },

    // Update summary section
    updateSummary(data) {
        const summaryContainer = document.getElementById('bucklensSummary');
        if (!summaryContainer) return;

        summaryContainer.className = 'mb-4 p-3 bucklens-summary rounded';
        summaryContainer.innerHTML = `
            <div class="row">
                <div class="col-md-8">
                    <h6 class="fw-semibold mb-2"><i class="fas fa-chart-line me-2"></i>Sightings Summary</h6>
                    <p class="mb-2">${data.bestOdds.summary}</p>
                </div>
                <div class="col-md-4">
                    <div class="text-end">
                        <div class="badge bg-secondary fs-6 mb-2">${data.totalSightings} Sightings</div><br>
                        <small class="text-muted">From ${data.totalTaggedPhotos} tagged photos</small>
                        <div class="mt-2">
                            <small class="text-muted">
                                <i class="fas fa-calendar-alt me-1"></i>
                                ${new Date(data.dateRange.start).toLocaleDateString()} - ${new Date(data.dateRange.end).toLocaleDateString()}
                            </small>
                        </div>
                    </div>
                </div>
            </div>
        `;
    },

    // Load camera chart
    async loadCameraChart(profileId) {
        const response = await fetch(`/profiles/${profileId}/analytics/charts/cameras`);
        if (!response.ok) throw new Error('Failed to load camera chart');
        
        const chartData = await response.json();
        this.createPieChart('cameraChart', chartData);
    },

    // Load time of day chart
    async loadTimeOfDayChart(profileId) {
        const response = await fetch(`/profiles/${profileId}/analytics/charts/timeofday`);
        if (!response.ok) throw new Error('Failed to load time of day chart');
        
        const chartData = await response.json();
        this.createPieChart('timeOfDayChart', chartData);
    },

    // Load moon phase chart
    async loadMoonPhaseChart(profileId) {
        const response = await fetch(`/profiles/${profileId}/analytics/charts/moonphase`);
        if (!response.ok) throw new Error('Failed to load moon phase chart');
        
        const chartData = await response.json();
        this.createBarChart('moonPhaseChart', chartData);
    },

    // Load wind direction chart
    async loadWindDirectionChart(profileId) {
        const response = await fetch(`/profiles/${profileId}/analytics/charts/winddirection`);
        if (!response.ok) throw new Error('Failed to load wind direction chart');
        
        const chartData = await response.json();
        this.createRadarChart('windDirectionChart', chartData);
    },

    // Load temperature chart
    async loadTemperatureChart(profileId) {
        const response = await fetch(`/profiles/${profileId}/analytics/charts/temperature`);
        if (!response.ok) throw new Error('Failed to load temperature chart');
        
        const chartData = await response.json();
        this.createBarChart('temperatureChart', chartData);
    },

    // Load sighting heatmap
    async loadSightingHeatmap(profileId) {
        const container = document.getElementById('sightingHeatmap');
        if (!container) {
            console.error('Heatmap container not found');
            return;
        }

        // Show loading state
        container.innerHTML = `
            <div class="d-flex align-items-center justify-content-center text-center p-4" style="min-height: 250px;">
                <div>
                    <div class="spinner-border text-primary mb-2" role="status"></div>
                    <p class="text-muted mb-0">Loading sighting locations...</p>
                </div>
            </div>
        `;

        try {
            console.log('Loading sighting heatmap for profile:', profileId);
            const response = await fetch(`/profiles/${profileId}/analytics/sightings/locations`);
            
            if (!response.ok) {
                console.error('Failed to load sighting locations, status:', response.status);
                throw new Error(`HTTP ${response.status}: ${response.statusText}`);
            }
            
            const locationData = await response.json();
            console.log('Raw API response:', locationData);
            console.log('Location data type:', typeof locationData);
            console.log('Is array:', Array.isArray(locationData));
            console.log('Number of sighting locations:', locationData ? locationData.length : 0);
            
            // Debug each location point
            if (locationData && locationData.length > 0) {
                locationData.forEach((point, index) => {
                    console.log(`Location ${index}:`, {
                        lat: point.latitude,
                        lng: point.longitude,
                        camera: point.cameraName,
                        date: point.dateTaken
                    });
                });
            }
            
            this.createSightingHeatmap(locationData || []);
        } catch (error) {
            console.error('Error loading sighting heatmap:', error);
            // Show detailed error in the heatmap container
            container.innerHTML = `
                <div class="d-flex align-items-center justify-content-center text-center p-4" style="min-height: 250px;">
                    <div>
                        <i class="fas fa-exclamation-triangle text-warning fa-2x mb-2"></i>
                        <p class="text-muted mb-2">Failed to load sighting locations</p>
                        <small class="text-muted d-block">${error.message}</small>
                        <button class="btn btn-sm btn-outline-primary mt-2" onclick="BuckLensCharts.loadSightingHeatmap(${profileId})">
                            <i class="fas fa-redo me-1"></i>Retry
                        </button>
                    </div>
                </div>
            `;
        }
    },

    // Create bar chart with optional moon phase icons
    createBarChart(canvasId, chartData) {
        const canvas = document.getElementById(canvasId);
        if (!canvas) {
            console.error(`Canvas element '${canvasId}' not found`);
            this.showChartError(canvasId, `Chart container not found`);
            return;
        }

        // Destroy existing chart if it exists
        if (this.chartInstances[canvasId]) {
            this.chartInstances[canvasId].destroy();
        }

        const ctx = canvas.getContext('2d');
        if (!ctx) {
            console.error(`Failed to get 2D context for canvas '${canvasId}'`);
            this.showChartError(canvasId, `Failed to initialize chart`);
            return;
        }

        // Check if this is a moon phase chart to add icons
        const isMoonPhaseChart = chartData.title.includes('Moon Phase');
        
        const chartConfig = {
            type: 'bar',
            data: {
                labels: chartData.dataPoints.map(p => p.label),
                datasets: [{
                    label: 'Sightings',
                    data: chartData.dataPoints.map(p => p.value),
                    backgroundColor: chartData.dataPoints.map((p, index) => 
                        this.colorSchemes.greenShades[index % this.colorSchemes.greenShades.length]
                    ),
                    borderColor: this.colors.primary,
                    borderWidth: 1
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    title: {
                        display: true,
                        text: chartData.title,
                        color: this.colors.dark
                    },
                    legend: {
                        display: false
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        ticks: {
                            stepSize: 1
                        }
                    },
                    x: {
                        ticks: {
                            maxRotation: 45,
                            minRotation: 0,
                            // Hide x-axis labels for moon phase chart since icons below will serve as labels
                            display: !isMoonPhaseChart
                        }
                    }
                },
                onClick: (event, elements) => {
                    if (elements.length > 0) {
                        const index = elements[0].index;
                        const dataPoint = chartData.dataPoints[index];
                        this.handleChartClick(chartData.type, dataPoint);
                    }
                }
            }
        };

        try {
            this.chartInstances[canvasId] = new Chart(ctx, chartConfig);
            
            // For moon phase chart, also add icons below the canvas using DOM manipulation
            if (isMoonPhaseChart) {
                setTimeout(() => this.addMoonPhaseIconsBelow(canvasId, chartData), 100);
            }
        } catch (error) {
            console.error(`Error creating bar chart '${canvasId}':`, error);
            this.showChartError(canvasId, `Failed to render chart`);
        }
    },

    // Add unified moon phase icons with labels below the canvas
    addMoonPhaseIconsBelow(canvasId, chartData) {
        const canvas = document.getElementById(canvasId);
        if (!canvas) return;
        
        const container = canvas.closest('.bucklens-chart-container');
        if (!container) return;
        
        // Add moon-phase-chart class for special styling
        container.classList.add('moon-phase-chart');
        
        // Remove existing icon container
        const existingIcons = container.querySelector('.moon-phase-icons');
        if (existingIcons) {
            existingIcons.remove();
        }
        
        // Create unified icon container
        const iconContainer = document.createElement('div');
        iconContainer.className = 'moon-phase-icons d-flex justify-content-around align-items-center mt-3 pt-2';
        iconContainer.style.fontSize = '28px'; // Larger icons for better visibility
        
        // Add icons for each data point
        chartData.dataPoints.forEach(point => {
            const iconDiv = document.createElement('div');
            iconDiv.className = 'text-center';
            iconDiv.style.color = this.colors.primary;
            
            const iconHtml = this.moonPhaseIcons[point.label] || '<i class="wi wi-moon-alt"></i>';
            iconDiv.innerHTML = `
                <div style="margin-bottom: 4px;">${iconHtml}</div>
                <div class="text-muted" style="font-size: 8px; line-height: 1; font-weight: 500;">${point.label}</div>
            `;
            
            iconContainer.appendChild(iconDiv);
        });
        
        container.appendChild(iconContainer);
    },

    // Create pie chart
    createPieChart(canvasId, chartData) {
        const canvas = document.getElementById(canvasId);
        if (!canvas) {
            console.error(`Canvas element '${canvasId}' not found`);
            this.showChartError(canvasId, `Chart container not found`);
            return;
        }

        // Destroy existing chart if it exists
        if (this.chartInstances[canvasId]) {
            this.chartInstances[canvasId].destroy();
        }

        const ctx = canvas.getContext('2d');
        if (!ctx) {
            console.error(`Failed to get 2D context for canvas '${canvasId}'`);
            this.showChartError(canvasId, `Failed to initialize chart`);
            return;
        }

        const colors = chartData.dataPoints.map((p, index) => 
            this.colorSchemes.greenShades[index % this.colorSchemes.greenShades.length]
        );

        try {
            this.chartInstances[canvasId] = new Chart(ctx, {
                type: 'pie',
                data: {
                    labels: chartData.dataPoints.map(p => p.label),
                    datasets: [{
                        data: chartData.dataPoints.map(p => p.value),
                        backgroundColor: colors,
                        borderColor: '#fff',
                        borderWidth: 2
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: {
                        title: {
                            display: true,
                            text: chartData.title,
                            color: this.colors.dark
                        },
                        legend: {
                            position: 'bottom'
                        }
                    },
                    onClick: (event, elements) => {
                        if (elements.length > 0) {
                            const index = elements[0].index;
                            const dataPoint = chartData.dataPoints[index];
                            this.handleChartClick(chartData.type, dataPoint);
                        }
                    }
                }
            });
        } catch (error) {
            console.error(`Error creating pie chart '${canvasId}':`, error);
            this.showChartError(canvasId, `Failed to render chart`);
        }
    },

    // Create radar chart with wind speed data
    createRadarChart(canvasId, chartData) {
        const canvas = document.getElementById(canvasId);
        if (!canvas) {
            console.error(`Canvas element '${canvasId}' not found`);
            this.showChartError(canvasId, `Chart container not found`);
            return;
        }

        // Destroy existing chart if it exists
        if (this.chartInstances[canvasId]) {
            this.chartInstances[canvasId].destroy();
        }

        const ctx = canvas.getContext('2d');
        if (!ctx) {
            console.error(`Failed to get 2D context for canvas '${canvasId}'`);
            this.showChartError(canvasId, `Failed to initialize chart`);
            return;
        }

        try {
            this.chartInstances[canvasId] = new Chart(ctx, {
                type: 'radar',
                data: {
                    labels: chartData.dataPoints.map(p => p.label),
                    datasets: [{
                        label: 'Sightings',
                        data: chartData.dataPoints.map(p => p.value),
                        fill: true,
                        backgroundColor: this.colors.primary + '33',
                        borderColor: this.colors.primary,
                        pointBackgroundColor: this.colors.primary,
                        pointBorderColor: '#fff',
                        pointHoverBackgroundColor: '#fff',
                        pointHoverBorderColor: this.colors.primary
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: {
                        title: {
                            display: true,
                            text: chartData.title,
                            color: this.colors.dark
                        },
                        legend: {
                            display: false
                        },
                        tooltip: {
                            callbacks: {
                                afterLabel: function(context) {
                                    const dataPoint = chartData.dataPoints[context.dataIndex];
                                    const avgSpeed = dataPoint.metadata?.avgWindSpeed;
                                    if (avgSpeed !== undefined) {
                                        return `Avg Speed: ${avgSpeed} mph`;
                                    }
                                    return '';
                                }
                            }
                        }
                    },
                    scales: {
                        r: {
                            beginAtZero: true,
                            ticks: {
                                stepSize: 1
                            }
                        }
                    },
                    onClick: (event, elements) => {
                        if (elements.length > 0) {
                            const index = elements[0].index;
                            const dataPoint = chartData.dataPoints[index];
                            this.handleChartClick(chartData.type, dataPoint);
                        }
                    }
                }
            });

            // Add wind speed legend below the chart
            setTimeout(() => this.addWindSpeedLegend(canvasId, chartData), 100);
        } catch (error) {
            console.error(`Error creating radar chart '${canvasId}':`, error);
            this.showChartError(canvasId, `Failed to render chart`);
        }
    },

    // Add wind speed legend below the wind direction chart
    addWindSpeedLegend(canvasId, chartData) {
        const canvas = document.getElementById(canvasId);
        if (!canvas) return;
        
        const container = canvas.closest('.bucklens-chart-container');
        if (!container) return;
        
        // Remove existing legend
        const existingLegend = container.querySelector('.wind-speed-legend');
        if (existingLegend) {
            existingLegend.remove();
        }
        
        // Create legend showing wind speed ranges
        const legendContainer = document.createElement('div');
        legendContainer.className = 'wind-speed-legend mt-2 pt-2 border-top';
        
        // Create speed range indicators
        const speedRanges = [
            { label: '< 1 mph', color: '#D4E6D4' },
            { label: '1-4 mph', color: '#B8D6B8' },
            { label: '4-8 mph', color: '#9CC29C' },
            { label: '8-12 mph', color: '#8CAF8C' },
            { label: '12-16 mph', color: '#6B8E6B' },
            { label: '16-20 mph', color: '#527A52' },
            { label: '> 20 mph', color: '#3E5A3E' }
        ];
        
        let legendHtml = '<div class="d-flex flex-wrap justify-content-center gap-2">';
        speedRanges.forEach(range => {
            legendHtml += `
                <div class="d-flex align-items-center">
                    <div class="me-1" style="width: 12px; height: 12px; background-color: ${range.color}; border-radius: 2px;"></div>
                    <small class="text-muted">${range.label}</small>
                </div>
            `;
        });
        legendHtml += '</div>';
        
        legendContainer.innerHTML = legendHtml;
        container.appendChild(legendContainer);
    },

    // Create sighting heatmap using Mapbox
    createSightingHeatmap(locationData) {
        const container = document.getElementById('sightingHeatmap');
        if (!container) {
            console.error('Sighting heatmap container not found');
            return;
        }

        try {
            console.log('Creating heatmap with location data:', locationData);
            console.log('Data type:', typeof locationData, 'Is array:', Array.isArray(locationData));
            
            // Check if we have any location data
            if (!locationData || locationData.length === 0) {
                container.innerHTML = `
                    <div class="d-flex align-items-center justify-content-center text-center p-4" style="min-height: 250px;">
                        <div>
                            <i class="fas fa-map-marked-alt text-muted fa-2x mb-2"></i>
                            <p class="text-muted mb-2">No location data available</p>
                            <small class="text-muted d-block">Sightings need camera location data to appear on the heatmap</small>
                            <small class="text-muted">Check that your cameras have GPS coordinates configured</small>
                        </div>
                    </div>
                `;
                return;
            }

            // Filter out points without valid coordinates and debug each point
            const validLocations = locationData.filter((point, index) => {
                const hasLat = point.latitude != null && !isNaN(point.latitude);
                const hasLng = point.longitude != null && !isNaN(point.longitude);
                const isValid = hasLat && hasLng;
                
                console.log(`Location ${index}:`, {
                    camera: point.cameraName,
                    lat: point.latitude,
                    lng: point.longitude,
                    hasLat,
                    hasLng,
                    isValid
                });
                
                return isValid;
            });

            console.log('Filtered to', validLocations.length, 'valid locations out of', locationData.length, 'total');

            if (validLocations.length === 0) {
                container.innerHTML = `
                    <div class="d-flex align-items-center justify-content-center text-center p-4" style="min-height: 250px;">
                        <div>
                            <i class="fas fa-map-marked-alt text-muted fa-2x mb-2"></i>
                            <p class="text-muted mb-2">No valid coordinates found</p>
                            <small class="text-muted d-block">Found ${locationData.length} sighting(s), but none have GPS coordinates</small>
                            <small class="text-muted">Camera locations must be configured to show on the heatmap</small>
                            <div class="mt-3">
                                <details class="text-start">
                                    <summary class="text-muted small">Show debug info</summary>
                                    <pre class="small mt-2">${JSON.stringify(locationData, null, 2)}</pre>
                                </details>
                            </div>
                        </div>
                    </div>
                `;
                return;
            }

            // Create a unique map container ID to avoid conflicts
            const mapId = 'heatmap-' + Date.now();
            container.innerHTML = `
                <div id="${mapId}" style="width: 100%; height: 250px; border-radius: 8px; position: relative;">
                    <div class="d-flex align-items-center justify-content-center text-center" style="position: absolute; top: 0; left: 0; right: 0; bottom: 0; background: rgba(255,255,255,0.9); z-index: 1000;" id="${mapId}-loading">
                        <div>
                            <div class="spinner-border text-primary mb-2" role="status">
                                <span class="visually-hidden">Loading...</span>
                            </div>
                            <p class="text-muted mb-0">Loading map...</p>
                        </div>
                    </div>
                </div>
            `;

            // Wait for the container to be in the DOM
            setTimeout(() => {
                const mapContainer = document.getElementById(mapId);
                if (!mapContainer) return;

                // Calculate bounds from the valid location data
                const lats = validLocations.map(d => d.latitude);
                const lngs = validLocations.map(d => d.longitude);
                
                console.log('Valid latitudes:', lats);
                console.log('Valid longitudes:', lngs);
                
                const bounds = [
                    [Math.min(...lngs), Math.min(...lats)], // Southwest corner
                    [Math.max(...lngs), Math.max(...lats)]  // Northeast corner
                ];

                // Add padding to bounds
                const latRange = Math.max(...lats) - Math.min(...lats);
                const lngRange = Math.max(...lngs) - Math.min(...lngs);
                const padding = Math.max(latRange, lngRange) * 0.1; // 10% padding

                bounds[0][0] -= padding; // West
                bounds[0][1] -= padding; // South
                bounds[1][0] += padding; // East
                bounds[1][1] += padding; // North
                
                console.log('Calculated map bounds:', bounds);
                console.log('Coordinate ranges - Lat:', latRange, 'Lng:', lngRange);

                // Get Mapbox token
                const token = this.getMapboxToken();
                if (!token) {
                    console.warn('Mapbox token not found for heatmap - showing fallback display');
                    // Show sighting locations as cards instead of map
                    container.innerHTML = `
                        <div class="p-4" style="min-height: 250px;">
                            <div class="text-center mb-3">
                                <i class="fas fa-map-marked-alt text-muted fa-2x mb-2"></i>
                                <p class="text-muted mb-2">Map unavailable - showing locations as list</p>
                                <small class="text-muted">Mapbox configuration needed for map display</small>
                            </div>
                            <div class="row g-2">
                                ${validLocations.map(point => `
                                    <div class="col-md-6">
                                        <div class="card card-body text-start">
                                            <strong class="text-success">${point.cameraName}</strong>
                                            <small class="text-muted">${point.dateTaken}</small>
                                            ${point.latitude && point.longitude ? `<small class="text-muted">📍 ${point.latitude.toFixed(4)}, ${point.longitude.toFixed(4)}</small>` : ''}
                                            ${point.temperature ? `<small class="text-muted">🌡️ ${point.temperature}°F</small>` : ''}
                                            ${point.windDirection ? `<small class="text-muted">💨 ${point.windDirection}</small>` : ''}
                                            ${point.moonPhase ? `<small class="text-muted">🌙 ${point.moonPhase}</small>` : ''}
                                        </div>
                                    </div>
                                `).join('')}
                            </div>
                        </div>
                    `;
                    return;
                }

                console.log('Creating heatmap with', validLocations.length, 'valid sighting locations');

                // Initialize Mapbox map with different strategies for single vs multiple points
                mapboxgl.accessToken = token;
                
                let mapConfig = {
                    container: mapId,
                    style: 'mapbox://styles/mapbox/light-v11' // Lighter style for better heatmap visibility
                };
                
                if (validLocations.length === 1) {
                    // For single point, center on the location with a reasonable zoom
                    mapConfig.center = [validLocations[0].longitude, validLocations[0].latitude];
                    mapConfig.zoom = 12;
                    console.log('Single point detected, centering map at:', mapConfig.center);
                } else {
                    // For multiple points, use bounds
                    mapConfig.bounds = bounds;
                    mapConfig.fitBoundsOptions = { padding: 20 };
                    console.log('Multiple points detected, using bounds:', bounds);
                }
                
                const map = new mapboxgl.Map(mapConfig);

                // Add error handling for map loading failures
                map.on('error', (error) => {
                    console.error('Mapbox error:', error);
                    container.innerHTML = `
                        <div class="d-flex align-items-center justify-content-center text-center p-4" style="min-height: 250px;">
                            <div>
                                <i class="fas fa-exclamation-triangle text-warning fa-2x mb-2"></i>
                                <p class="text-muted mb-0">Map failed to load</p>
                                <small class="text-muted">Please check your internet connection or try refreshing the page</small>
                            </div>
                        </div>
                    `;
                });

                // Add timeout fallback in case map never loads
                const loadTimeout = setTimeout(() => {
                    if (map.loaded && !map.loaded()) {
                        console.warn('Map load timeout - showing fallback');
                        container.innerHTML = `
                            <div class="d-flex align-items-center justify-content-center text-center p-4" style="min-height: 250px;">
                                <div>
                                    <i class="fas fa-clock text-warning fa-2x mb-2"></i>
                                    <p class="text-muted mb-0">Map taking too long to load</p>
                                    <small class="text-muted">Showing sighting locations as a list instead</small>
                                    <div class="mt-3">
                                        ${validLocations.map(point => `
                                            <div class="card card-body mb-2 text-start">
                                                <strong>${point.cameraName}</strong><br>
                                                <small class="text-muted">${point.dateTaken}</small>
                                                ${point.temperature ? `<br><small>Temperature: ${point.temperature}°F</small>` : ''}
                                                ${point.windDirection ? `<br><small>Wind: ${point.windDirection}</small>` : ''}
                                            </div>
                                        `).join('')}
                                    </div>
                                </div>
                            </div>
                        `;
                    }
                }, 10000); // 10 second timeout

                map.on('load', () => {
                    // Clear the timeout since map loaded successfully
                    clearTimeout(loadTimeout);
                    
                    // Remove loading indicator
                    const loadingIndicator = document.getElementById(`${mapId}-loading`);
                    if (loadingIndicator) {
                        loadingIndicator.remove();
                    }
                    
                    console.log('Mapbox map loaded successfully');
                    console.log('Valid location data for heatmap:', validLocations);
                    
                    // Convert location data to GeoJSON format for heatmap
                    const geojsonData = {
                        type: 'FeatureCollection',
                        features: validLocations.map(point => ({
                            type: 'Feature',
                            properties: {
                                photoId: point.photoId,
                                dateTaken: point.dateTaken,
                                cameraName: point.cameraName,
                                temperature: point.temperature,
                                windDirection: point.windDirection,
                                moonPhase: point.moonPhase
                            },
                            geometry: {
                                type: 'Point',
                                coordinates: [point.longitude, point.latitude]
                            }
                        }))
                    };

                    // Add the data source
                    map.addSource('sightings', {
                        type: 'geojson',
                        data: geojsonData
                    });

                    console.log('Added data source with', geojsonData.features.length, 'features');

                    // Add heatmap layer - make more visible for small datasets
                    map.addLayer({
                        id: 'sightings-heatmap',
                        type: 'heatmap',
                        source: 'sightings',
                        maxzoom: 15,
                        paint: {
                            // Increase weight to make heatmap more visible
                            'heatmap-weight': [
                                'interpolate',
                                ['linear'],
                                ['zoom'],
                                0, 1,
                                9, 3
                            ],
                            // Color ramp - make more vibrant and visible
                            'heatmap-color': [
                                'interpolate',
                                ['linear'],
                                ['heatmap-density'],
                                0, 'rgba(82, 122, 82, 0)',        // Transparent at low density
                                0.2, 'rgba(82, 122, 82, 0.4)',   // Primary green, higher opacity  
                                0.4, 'rgba(107, 142, 107, 0.6)', // Lighter green
                                0.6, 'rgba(140, 175, 140, 0.8)', // Even lighter green
                                0.8, 'rgba(78, 115, 78, 0.9)',   // Darker green
                                1, 'rgba(60, 90, 60, 1)'         // Very dark green for hotspots
                            ],
                            // Increase heatmap radius to make it more visible
                            'heatmap-radius': [
                                'interpolate',
                                ['linear'],
                                ['zoom'],
                                0, 30,   // Larger radius at low zoom
                                9, 50,   // Even larger radius
                                16, 80   // Much larger radius at high zoom
                            ],
                            // Adjust the heatmap opacity
                            'heatmap-opacity': [
                                'interpolate',
                                ['linear'],
                                ['zoom'],
                                7, 0.9,
                                15, 0.7
                            ]
                        }
                    });

                    console.log('Added heatmap layer');

                    // Add individual points - always visible for small datasets
                    map.addLayer({
                        id: 'sightings-points',
                        type: 'circle',
                        source: 'sightings',
                        maxzoom: 15, // Show points up to zoom level 15, then let heatmap take over
                        paint: {
                            'circle-radius': [
                                'interpolate',
                                ['linear'],
                                ['zoom'],
                                5, 8,    // Larger at low zoom
                                10, 15,  // Even larger at medium zoom
                                15, 25   // Very large at high zoom
                            ],
                            'circle-color': '#527A52',
                            'circle-stroke-width': 3,
                            'circle-stroke-color': '#ffffff',
                            'circle-opacity': 1.0  // Full opacity for maximum visibility
                        }
                    });

                    console.log('Added circle points layer');

                    // Add popup on click
                    map.on('click', 'sightings-points', (e) => {
                        const properties = e.features[0].properties;
                        const popup = new mapboxgl.Popup()
                            .setLngLat(e.lngLat)
                            .setHTML(`
                                <div class="p-2">
                                    <h6 class="mb-1">${properties.cameraName}</h6>
                                    <small class="text-muted d-block">${properties.dateTaken}</small>
                                    ${properties.temperature ? `<small class="text-muted d-block">Temperature: ${properties.temperature}°F</small>` : ''}
                                    ${properties.windDirection ? `<small class="text-muted d-block">Wind: ${properties.windDirection}</small>` : ''}
                                    ${properties.moonPhase ? `<small class="text-muted d-block">Moon: ${properties.moonPhase}</small>` : ''}
                                </div>
                            `)
                            .addTo(map);
                    });

                    // Change cursor on hover
                    map.on('mouseenter', 'sightings-points', () => {
                        map.getCanvas().style.cursor = 'pointer';
                    });

                    map.on('mouseleave', 'sightings-points', () => {
                        map.getCanvas().style.cursor = '';
                    });
                });

                // Store the map instance for cleanup
                if (!this.mapInstances) {
                    this.mapInstances = {};
                }
                this.mapInstances[mapId] = map;

            }, 100);

        } catch (error) {
            console.error('Error creating sighting heatmap:', error);
            container.innerHTML = `
                <div class="d-flex align-items-center justify-content-center text-center p-4" style="min-height: 250px;">
                    <div>
                        <i class="fas fa-exclamation-triangle text-warning fa-2x mb-2"></i>
                        <p class="text-muted mb-0">Failed to load heatmap</p>
                        <small class="text-muted">${error.message}</small>
                    </div>
                </div>
            `;
        }
    },

    // Get Mapbox token from various sources
    getMapboxToken() {
        // Try meta tag first
        const meta = document.querySelector('meta[name="mapbox-token"]');
        if (meta?.content) return meta.content;

        // Try global variable
        if (window.__MAPBOX_TOKEN) return window.__MAPBOX_TOKEN;

        // Try existing mapbox instance
        if (typeof mapboxgl !== 'undefined' && mapboxgl.accessToken) {
            return mapboxgl.accessToken;
        }

        return null;
    },

    // Handle chart click events for drilldown
    handleChartClick(chartType, dataPoint) {
        console.log('Chart clicked:', chartType, dataPoint);
        // TODO: Implement drilldown functionality
        // This could show a modal with detailed sighting information
    },

    // Export chart data
    async exportData(format, profileId) {
        if (!profileId) {
            profileId = document.querySelector('[data-profile-id]')?.getAttribute('data-profile-id');
        }
        
        if (!profileId) {
            alert('Profile ID not found');
            return;
        }

        try {
            const response = await fetch(`/profiles/${profileId}/analytics/summary`);
            if (!response.ok) throw new Error('Failed to load data');
            
            const data = await response.json();
            
            if (format === 'csv') {
                this.downloadCSV(data);
            } else if (format === 'json') {
                this.downloadJSON(data);
            }
        } catch (error) {
            console.error('Export error:', error);
            alert('Failed to export data. Please try again.');
        }
    },

    // Download data as CSV
    downloadCSV(data) {
        const csvContent = [
            ['Metric', 'Value'],
            ['Total Sightings', data.totalSightings],
            ['Total Tagged Photos', data.totalTaggedPhotos],
            ['Date Range Start', data.dateRange.start],
            ['Date Range End', data.dateRange.end],
            ['Best Time of Day', data.bestOdds.bestTimeOfDay || 'N/A'],
            ['Best Camera', data.bestOdds.bestCamera || 'N/A'],
            ['Best Moon Phase', data.bestOdds.bestMoonPhase || 'N/A'],
            ['Best Wind Direction', data.bestOdds.bestWindDirection || 'N/A'],
            ['Best Temperature Range', data.bestOdds.bestTemperatureRange || 'N/A']
        ].map(row => row.map(field => `"${field}"`).join(',')).join('\n');

        const blob = new Blob([csvContent], { type: 'text/csv' });
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `bucklens-analytics-${data.profileId}.csv`;
        document.body.appendChild(a);
        a.click();
        window.URL.revokeObjectURL(url);
        document.body.removeChild(a);
    },

    // Download data as JSON
    downloadJSON(data) {
        const jsonContent = JSON.stringify(data, null, 2);
        const blob = new Blob([jsonContent], { type: 'application/json' });
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `bucklens-analytics-${data.profileId}.json`;
        document.body.appendChild(a);
        a.click();
        window.URL.revokeObjectURL(url);
        document.body.removeChild(a);
    },

    // Show loading state
    showLoading() {
        // Show loading in the summary section only
        const summaryContainer = document.getElementById('bucklensSummary');
        if (summaryContainer) {
            summaryContainer.innerHTML = `
                <div class="bucklens-chart-loading text-center">
                    <div class="spinner-border text-primary" role="status" style="width: 2rem; height: 2rem;">
                        <span class="visually-hidden">Loading...</span>
                    </div>
                    <p class="mt-2 mb-0 text-muted">
                        <i class="fas fa-chart-line me-2"></i>Loading analytics data...
                    </p>
                    <small class="text-muted">Analyzing sightings and weather patterns</small>
                </div>
            `;
        }

        // Add loading overlay to chart containers
        const chartCards = document.querySelectorAll('.bucklens-chart-card .card-body');
        chartCards.forEach(cardBody => {
            const existingOverlay = cardBody.querySelector('.chart-loading-overlay');
            if (!existingOverlay) {
                const overlay = document.createElement('div');
                overlay.className = 'chart-loading-overlay position-absolute top-0 start-0 w-100 h-100 d-flex align-items-center justify-content-center bg-white bg-opacity-75';
                overlay.innerHTML = `
                    <div class="spinner-border text-primary" role="status">
                        <span class="visually-hidden">Loading...</span>
                    </div>
                `;
                cardBody.style.position = 'relative';
                cardBody.appendChild(overlay);
            }
        });
    },

    // Hide loading state and restore charts container
    hideLoading() {
        // Remove loading overlays from chart containers
        const overlays = document.querySelectorAll('.chart-loading-overlay');
        overlays.forEach(overlay => overlay.remove());

        // Reset any inline styles that were added
        const chartCards = document.querySelectorAll('.bucklens-chart-card .card-body');
        chartCards.forEach(cardBody => {
            cardBody.style.position = '';
        });
    },

    // Show error message
    showError(message) {
        const container = document.getElementById('bucklensCharts');
        if (container) {
            container.innerHTML = `
                <div class="alert alert-danger" role="alert">
                    <i class="fas fa-exclamation-triangle"></i> ${message}
                </div>
            `;
        }
    },

    // Show error for individual chart
    showChartError(canvasId, message) {
        const canvas = document.getElementById(canvasId);
        if (canvas) {
            const container = canvas.closest('.bucklens-chart-container') || canvas.closest('.card-body');
            if (container) {
                container.innerHTML = `
                    <div class="d-flex align-items-center justify-content-center text-center p-4" style="min-height: 200px;">
                        <div>
                            <i class="fas fa-exclamation-triangle text-warning fa-2x mb-2"></i>
                            <p class="text-muted mb-0">${message}</p>
                            <small class="text-muted">Chart ID: ${canvasId}</small>
                        </div>
                    </div>
                `;
            }
        }
    },

    // Cleanup charts when leaving page
    destroy() {
        Object.values(this.chartInstances).forEach(chart => {
            if (chart) chart.destroy();
        });
        this.chartInstances = {};
        
        // Cleanup map instances
        if (this.mapInstances) {
            Object.values(this.mapInstances).forEach(map => {
                if (map) map.remove();
            });
            this.mapInstances = {};
        }
    }
};

// Auto-initialize when analytics accordion is shown
document.addEventListener('DOMContentLoaded', function() {
    const bucklensCollapse = document.getElementById('bucklensAnalyticsCollapse');
    if (bucklensCollapse) {
        bucklensCollapse.addEventListener('shown.bs.collapse', function() {
            const profileId = document.querySelector('[data-profile-id]')?.getAttribute('data-profile-id');
            if (profileId && !BuckLens.Charts.initialized) {
                BuckLens.Charts.initialized = true;
                BuckLens.Charts.init(profileId);
            }
        });
    }
});

// Cleanup on page unload
window.addEventListener('beforeunload', function() {
    if (window.BuckLens?.Charts) {
        BuckLens.Charts.destroy();
    }
});