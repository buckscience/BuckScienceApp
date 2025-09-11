// BuckEye Analytics Chart Components
window.BuckEye = window.BuckEye || {};

BuckEye.Charts = {
    // Store chart instances for cleanup
    chartInstances: {},

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

    // Moon phase icons mapping
    moonPhaseIcons: {
        'New Moon': '🌑',
        'Waxing Crescent': '🌒',
        'First Quarter': '🌓',
        'Waxing Gibbous': '🌔',
        'Full Moon': '🌕',
        'Waning Gibbous': '🌖',
        'Last Quarter': '🌗',
        'Waning Crescent': '🌘'
    },

    // Wind direction icons
    windDirectionIcons: {
        'N': '↑', 'NNE': '↗', 'NE': '↗', 'ENE': '↗',
        'E': '→', 'ESE': '↘', 'SE': '↘', 'SSE': '↘',
        'S': '↓', 'SSW': '↙', 'SW': '↙', 'WSW': '↙',
        'W': '←', 'WNW': '↖', 'NW': '↖', 'NNW': '↖'
    },

    // Chart color schemes
    colorSchemes: {
        default: ['#527A52', '#8CAF8C', '#4a5a5f', '#17a2b8', '#ffc107', '#dc3545', '#6f42c1', '#e83e8c'],
        timeOfDay: {
            'Morning': '#f39c12',
            'Midday': '#e74c3c',
            'Evening': '#9b59b6',
            'Night': '#2c3e50'
        },
        windDirection: {
            'N': '#3498db', 'NE': '#5dade2', 'E': '#85c1e9', 'SE': '#aed6f1',
            'S': '#f39c12', 'SW': '#f5b041', 'W': '#f7dc6f', 'NW': '#f8c471'
        }
    },

    // Initialize all charts for a profile
    async init(profileId) {
        try {
            // Show loading state
            this.showLoading();

            // Load summary data first
            await this.loadSummary(profileId);

            // Load all charts
            await Promise.all([
                this.loadCameraChart(profileId),
                this.loadTimeOfDayChart(profileId),
                this.loadMoonPhaseChart(profileId),
                this.loadWindDirectionChart(profileId),
                this.loadTemperatureChart(profileId)
            ]);

            // Load sighting locations for heatmap
            await this.loadSightingLocations(profileId);

            // Hide loading state
            this.hideLoading();
        } catch (error) {
            console.error('Error initializing BuckEye charts:', error);
            this.showError('Failed to load analytics data. Please try again.');
        }
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
        const summaryContainer = document.getElementById('buckeyeSummary');
        if (!summaryContainer) return;

        summaryContainer.className = 'mb-4 p-3 buckeye-summary rounded';
        summaryContainer.innerHTML = `
            <div class="row">
                <div class="col-md-8">
                    <h6 class="fw-semibold mb-2"><i class="fas fa-chart-line me-2"></i>Sightings Summary</h6>
                    <p class="mb-2">${data.bestOdds.summary}</p>
                    ${data.bestOdds.bestTimeOfDay ? `
                        <div class="mb-2">
                            <small class="text-muted">Best patterns identified:</small>
                            <ul class="small mb-0 mt-1">
                                ${data.bestOdds.bestTimeOfDay ? `<li><strong><i class="fas fa-clock me-1"></i>Time:</strong> ${data.bestOdds.bestTimeOfDay}</li>` : ''}
                                ${data.bestOdds.bestCamera ? `<li><strong><i class="fas fa-camera me-1"></i>Camera:</strong> ${data.bestOdds.bestCamera}</li>` : ''}
                                ${data.bestOdds.bestWindDirection ? `<li><strong><i class="fas fa-wind me-1"></i>Wind:</strong> ${data.bestOdds.bestWindDirection}</li>` : ''}
                                ${data.bestOdds.bestMoonPhase ? `<li><strong><span class="moon-phase-icon">${this.moonPhaseIcons[data.bestOdds.bestMoonPhase] || '🌙'}</span>Moon:</strong> ${data.bestOdds.bestMoonPhase}</li>` : ''}
                            </ul>
                        </div>
                    ` : ''}
                </div>
                <div class="col-md-4">
                    <div class="text-end">
                        <div class="badge bg-primary fs-6 mb-2">${data.totalSightings} Sightings</div><br>
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
        this.createBarChart('cameraChart', chartData);
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

    // Load sighting locations for heatmap
    async loadSightingLocations(profileId) {
        const response = await fetch(`/profiles/${profileId}/analytics/sightings/locations`);
        if (!response.ok) throw new Error('Failed to load sighting locations');
        
        const locations = await response.json();
        this.updateHeatmap(locations);
    },

    // Create bar chart
    createBarChart(canvasId, chartData) {
        const canvas = document.getElementById(canvasId);
        if (!canvas) return;

        // Destroy existing chart if it exists
        if (this.chartInstances[canvasId]) {
            this.chartInstances[canvasId].destroy();
        }

        const ctx = canvas.getContext('2d');
        this.chartInstances[canvasId] = new Chart(ctx, {
            type: 'bar',
            data: {
                labels: chartData.dataPoints.map(p => p.label),
                datasets: [{
                    label: 'Sightings',
                    data: chartData.dataPoints.map(p => p.value),
                    backgroundColor: this.colors.primary,
                    borderColor: this.colors.secondary,
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
    },

    // Create pie chart
    createPieChart(canvasId, chartData) {
        const canvas = document.getElementById(canvasId);
        if (!canvas) return;

        // Destroy existing chart if it exists
        if (this.chartInstances[canvasId]) {
            this.chartInstances[canvasId].destroy();
        }

        const colors = chartData.dataPoints.map(p => 
            this.colorSchemes.timeOfDay[p.label] || this.colorSchemes.default[chartData.dataPoints.indexOf(p) % this.colorSchemes.default.length]
        );

        const ctx = canvas.getContext('2d');
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
    },

    // Create radar chart
    createRadarChart(canvasId, chartData) {
        const canvas = document.getElementById(canvasId);
        if (!canvas) return;

        // Destroy existing chart if it exists
        if (this.chartInstances[canvasId]) {
            this.chartInstances[canvasId].destroy();
        }

        const ctx = canvas.getContext('2d');
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
    },

    // Update heatmap with sighting locations
    updateHeatmap(locations) {
        // This would integrate with the existing map system
        // For now, we'll create a simple location list
        const heatmapContainer = document.getElementById('sightingHeatmap');
        if (!heatmapContainer) return;

        if (locations.length === 0) {
            heatmapContainer.innerHTML = `
                <div class="text-center py-4">
                    <i class="fas fa-map-marker-alt fa-3x text-muted mb-3"></i>
                    <p class="text-muted">No location data available for sightings.</p>
                </div>
            `;
            return;
        }

        const locationGroups = locations.reduce((groups, location) => {
            const key = `${location.cameraName || 'Unknown Camera'}`;
            if (!groups[key]) groups[key] = [];
            groups[key].push(location);
            return groups;
        }, {});

        let html = '<div class="row g-2">';
        Object.entries(locationGroups).forEach(([camera, locs]) => {
            const latestLocation = locs[0]; // Most recent sighting
            html += `
                <div class="col-md-6">
                    <div class="buckeye-location-card p-3">
                        <div class="d-flex justify-content-between align-items-start">
                            <div>
                                <h6 class="mb-1">
                                    <i class="fas fa-camera me-2"></i>${camera}
                                </h6>
                                <small class="text-muted">
                                    <i class="fas fa-crosshairs me-1"></i>${locs.length} sighting${locs.length > 1 ? 's' : ''}
                                </small>
                                ${latestLocation.temperature ? `
                                    <div class="mt-1">
                                        <small class="temperature-bin">
                                            <i class="fas fa-thermometer-half me-1"></i>
                                            ${Math.round(latestLocation.temperature * 9/5 + 32)}°F
                                        </small>
                                    </div>
                                ` : ''}
                                ${latestLocation.windDirection ? `
                                    <div class="mt-1">
                                        <small class="text-muted">
                                            <span class="wind-direction-icon">${this.windDirectionIcons[latestLocation.windDirection] || '🧭'}</span>
                                            ${latestLocation.windDirection}
                                        </small>
                                    </div>
                                ` : ''}
                                ${latestLocation.moonPhase ? `
                                    <div class="mt-1">
                                        <small class="text-muted">
                                            <span class="moon-phase-icon">${this.moonPhaseIcons[latestLocation.moonPhase] || '🌙'}</span>
                                            ${latestLocation.moonPhase}
                                        </small>
                                    </div>
                                ` : ''}
                            </div>
                            <div class="text-end">
                                <button class="btn btn-sm btn-outline-primary" onclick="BuckEye.Charts.showLocationDetails('${camera}')">
                                    <i class="fas fa-map-marker-alt"></i>
                                </button>
                            </div>
                        </div>
                        <div class="mt-2">
                            <small class="text-muted">
                                <i class="fas fa-clock me-1"></i>Latest: ${new Date(latestLocation.dateTaken).toLocaleDateString()}
                            </small>
                        </div>
                    </div>
                </div>
            `;
        });
        html += '</div>';

        heatmapContainer.innerHTML = html;
    },

    // Handle chart click events for drilldown
    handleChartClick(chartType, dataPoint) {
        console.log('Chart clicked:', chartType, dataPoint);
        // TODO: Implement drilldown functionality
        // This could show a modal with detailed sighting information
    },

    // Show location details on map
    showLocationDetails(cameraName) {
        console.log('Show location details for:', cameraName);
        // TODO: Integrate with existing map functionality to highlight camera location
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
        a.download = `buckeye-analytics-${data.profileId}.csv`;
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
        a.download = `buckeye-analytics-${data.profileId}.json`;
        document.body.appendChild(a);
        a.click();
        window.URL.revokeObjectURL(url);
        document.body.removeChild(a);
    },

    // Show loading state
    showLoading() {
        const container = document.getElementById('buckeyeCharts');
        if (container) {
            container.innerHTML = `
                <div class="buckeye-chart-loading">
                    <div class="spinner-border text-primary" role="status" style="width: 3rem; height: 3rem;">
                        <span class="visually-hidden">Loading...</span>
                    </div>
                    <p class="mt-3 text-muted">
                        <i class="fas fa-chart-line me-2"></i>Loading analytics data...
                    </p>
                    <small class="text-muted">Analyzing sightings and weather patterns</small>
                </div>
            `;
        }
    },

    // Hide loading state
    hideLoading() {
        // Loading will be replaced by chart content
    },

    // Show error message
    showError(message) {
        const container = document.getElementById('buckeyeCharts');
        if (container) {
            container.innerHTML = `
                <div class="alert alert-danger" role="alert">
                    <i class="fas fa-exclamation-triangle"></i> ${message}
                </div>
            `;
        }
    },

    // Cleanup charts when leaving page
    destroy() {
        Object.values(this.chartInstances).forEach(chart => {
            if (chart) chart.destroy();
        });
        this.chartInstances = {};
    }
};

// Auto-initialize if profile ID is available
document.addEventListener('DOMContentLoaded', function() {
    const profileId = document.querySelector('[data-profile-id]')?.getAttribute('data-profile-id');
    if (profileId && document.getElementById('buckeyeAnalytics')) {
        BuckEye.Charts.init(profileId);
    }
});

// Cleanup on page unload
window.addEventListener('beforeunload', function() {
    if (window.BuckEye?.Charts) {
        BuckEye.Charts.destroy();
    }
});