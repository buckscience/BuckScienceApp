// BuckEye Analytics Chart Components
window.BuckEye = window.BuckEye || {};

BuckEye.Charts = {
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
                this.loadTemperatureChart(profileId)
            ]);

            // Hide loading state
            this.hideLoading();
        } catch (error) {
            console.error('Error initializing BuckEye charts:', error);
            this.showError('Failed to load analytics data. Please try again.');
        }
    },

    // Ensure all required canvas elements exist in the DOM
    ensureCanvasElements() {
        const chartsContainer = document.getElementById('buckeyeCharts');
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
        const container = document.getElementById('buckeyeCharts');
        if (!container) return;

        container.innerHTML = `
            <div class="row g-4">
                <!-- Sightings by Camera -->
                <div class="col-md-6">
                    <div class="card border-0 shadow-sm buckeye-chart-card">
                        <div class="card-body">
                            <div class="buckeye-chart-container">
                                <canvas id="cameraChart"></canvas>
                            </div>
                        </div>
                    </div>
                </div>

                <!-- Sightings by Time of Day -->
                <div class="col-md-6">
                    <div class="card border-0 shadow-sm buckeye-chart-card">
                        <div class="card-body">
                            <div class="buckeye-chart-container">
                                <canvas id="timeOfDayChart"></canvas>
                            </div>
                        </div>
                    </div>
                </div>

                <!-- Sightings by Moon Phase -->
                <div class="col-md-6">
                    <div class="card border-0 shadow-sm buckeye-chart-card">
                        <div class="card-body">
                            <div class="buckeye-chart-container">
                                <canvas id="moonPhaseChart"></canvas>
                            </div>
                        </div>
                    </div>
                </div>

                <!-- Sightings by Wind Direction -->
                <div class="col-md-6">
                    <div class="card border-0 shadow-sm buckeye-chart-card">
                        <div class="card-body">
                            <div class="buckeye-chart-container">
                                <canvas id="windDirectionChart"></canvas>
                            </div>
                        </div>
                    </div>
                </div>

                <!-- Sightings by Temperature -->
                <div class="col-md-6">
                    <div class="card border-0 shadow-sm buckeye-chart-card">
                        <div class="card-body">
                            <div class="buckeye-chart-container">
                                <canvas id="temperatureChart"></canvas>
                            </div>
                        </div>
                    </div>
                </div>

                <!-- Sighting Heatmap/Locations -->
                <div class="col-md-6">
                    <div class="card border-0 shadow-sm buckeye-chart-card">
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
                                ${data.bestOdds.bestMoonPhase ? `<li><strong><span class="moon-phase-icon">${this.moonPhaseIcons[data.bestOdds.bestMoonPhase] || '<i class="wi wi-moon-alt"></i>'}</span> Moon:</strong> ${data.bestOdds.bestMoonPhase}</li>` : ''}
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
                            minRotation: 0
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

        // Add moon phase icons plugin if this is the moon phase chart
        if (isMoonPhaseChart) {
            chartConfig.plugins = [{
                id: 'moonPhaseIcons',
                afterDraw: (chart) => {
                    const ctx = chart.ctx;
                    const chartArea = chart.chartArea;
                    const meta = chart.getDatasetMeta(0);
                    
                    meta.data.forEach((bar, index) => {
                        const label = chartData.dataPoints[index].label;
                        
                        // Calculate position below the bar
                        const x = bar.x;
                        const y = chartArea.bottom + 30;
                        
                        // Use Weather Icons font symbols for moon phases (monochrome)
                        ctx.save();
                        ctx.fillStyle = this.colors.primary;
                        ctx.font = '24px "Weather Icons"';
                        ctx.textAlign = 'center';
                        
                        // Map moon phases to Weather Icons Unicode symbols
                        const moonIconSymbols = {
                            'New Moon': '\uf095',
                            'Waxing Crescent': '\uf09c',
                            'First Quarter': '\uf09a',
                            'Waxing Gibbous': '\uf09d',
                            'Full Moon': '\uf097',
                            'Waning Gibbous': '\uf09e',
                            'Last Quarter': '\uf09b',
                            'Waning Crescent': '\uf09f'
                        };
                        
                        const symbol = moonIconSymbols[label] || '\uf095';
                        ctx.fillText(symbol, x, y);
                        ctx.restore();
                    });
                }
            }];
        }

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

    // Add moon phase icons below the canvas
    addMoonPhaseIconsBelow(canvasId, chartData) {
        const canvas = document.getElementById(canvasId);
        if (!canvas) return;
        
        const container = canvas.closest('.buckeye-chart-container');
        if (!container) return;
        
        // Add moon-phase-chart class for special styling
        container.classList.add('moon-phase-chart');
        
        // Remove existing icon container
        const existingIcons = container.querySelector('.moon-phase-icons');
        if (existingIcons) {
            existingIcons.remove();
        }
        
        // Create icon container
        const iconContainer = document.createElement('div');
        iconContainer.className = 'moon-phase-icons d-flex justify-content-around align-items-center mt-2 pt-2 border-top';
        iconContainer.style.fontSize = '20px';
        
        // Add icons for each data point
        chartData.dataPoints.forEach(point => {
            const iconDiv = document.createElement('div');
            iconDiv.className = 'text-center';
            iconDiv.style.color = this.colors.primary;
            
            const iconHtml = this.moonPhaseIcons[point.label] || '<i class="wi wi-moon-alt"></i>';
            iconDiv.innerHTML = `
                <div>${iconHtml}</div>
                <small class="text-muted d-block" style="font-size: 9px;">${point.label}</small>
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
        
        const container = canvas.closest('.buckeye-chart-container');
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
        // Show loading in the summary section only
        const summaryContainer = document.getElementById('buckeyeSummary');
        if (summaryContainer) {
            summaryContainer.innerHTML = `
                <div class="buckeye-chart-loading text-center">
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
        const chartCards = document.querySelectorAll('.buckeye-chart-card .card-body');
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
        const chartCards = document.querySelectorAll('.buckeye-chart-card .card-body');
        chartCards.forEach(cardBody => {
            cardBody.style.position = '';
        });
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

    // Show error for individual chart
    showChartError(canvasId, message) {
        const canvas = document.getElementById(canvasId);
        if (canvas) {
            const container = canvas.closest('.buckeye-chart-container') || canvas.closest('.card-body');
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
    }
};

// Auto-initialize when analytics accordion is shown
document.addEventListener('DOMContentLoaded', function() {
    const buckeyeCollapse = document.getElementById('buckeyeAnalyticsCollapse');
    if (buckeyeCollapse) {
        buckeyeCollapse.addEventListener('shown.bs.collapse', function() {
            const profileId = document.querySelector('[data-profile-id]')?.getAttribute('data-profile-id');
            if (profileId && !BuckEye.Charts.initialized) {
                BuckEye.Charts.initialized = true;
                BuckEye.Charts.init(profileId);
            }
        });
    }
});

// Cleanup on page unload
window.addEventListener('beforeunload', function() {
    if (window.BuckEye?.Charts) {
        BuckEye.Charts.destroy();
    }
});