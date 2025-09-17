// Feature classification utilities for Buck Science App
window.FeatureUtils = (function() {
    
    // Map of ClassificationType enum values to their properties
    const featureConfig = {
        // Topographical Features
        1: { name: 'Ridge', category: 'Topographical', color: '#8B4513' },
        2: { name: 'Ridge Point', category: 'Topographical', color: '#8B4513' },
        3: { name: 'Ridge Spur', category: 'Topographical', color: '#8B4513' },
        4: { name: 'Saddle', category: 'Topographical', color: '#8B4513' },
        5: { name: 'Bench', category: 'Topographical', color: '#8B4513' },
        6: { name: 'Draw', category: 'Topographical', color: '#8B4513' },
        7: { name: 'Creek Crossing', category: 'Topographical', color: '#8B4513' },
        8: { name: 'Ditch', category: 'Topographical', color: '#8B4513' },
        9: { name: 'Valley', category: 'Topographical', color: '#8B4513' },
        10: { name: 'Bluff', category: 'Topographical', color: '#8B4513' },
        11: { name: 'Field Edge', category: 'Topographical', color: '#8B4513' },
        12: { name: 'Inside Corner', category: 'Topographical', color: '#8B4513' },
        13: { name: 'Peninsula', category: 'Topographical', color: '#8B4513' },
        14: { name: 'Island', category: 'Topographical', color: '#8B4513' },
        15: { name: 'Pinch Point/Funnel', category: 'Topographical', color: '#FF8C00' },
        16: { name: 'Travel Corridor', category: 'Topographical', color: '#FF6347' },
        17: { name: 'Spur', category: 'Topographical', color: '#8B4513' },
        18: { name: 'Knob', category: 'Topographical', color: '#8B4513' },

        // Food Resources
        31: { name: 'Agricultural Crop Field', category: 'Food Resources', color: '#32CD32' },
        32: { name: 'Food Plot', category: 'Food Resources', color: '#32CD32' },
        33: { name: 'Mast Tree Patch', category: 'Food Resources', color: '#32CD32' },
        34: { name: 'Browse Patch', category: 'Food Resources', color: '#32CD32' },
        35: { name: 'Prairie Forb Patch', category: 'Food Resources', color: '#32CD32' },

        // Water Resources
        51: { name: 'Creek', category: 'Water Resources', color: '#1E90FF' },
        52: { name: 'Pond', category: 'Water Resources', color: '#1E90FF' },
        53: { name: 'Lake', category: 'Water Resources', color: '#1E90FF' },
        54: { name: 'Spring', category: 'Water Resources', color: '#1E90FF' },
        55: { name: 'Waterhole', category: 'Water Resources', color: '#1E90FF' },
        56: { name: 'Trough', category: 'Water Resources', color: '#1E90FF' },

        // Bedding & Cover Resources
        70: { name: 'Bedding Area', category: 'Bedding & Cover', color: '#228B22' },
        71: { name: 'Thick Brush', category: 'Bedding & Cover', color: '#228B22' },
        72: { name: 'Clearcut', category: 'Bedding & Cover', color: '#228B22' },
        73: { name: 'CRP/Conservation Reserve', category: 'Bedding & Cover', color: '#228B22' },
        74: { name: 'Swamp', category: 'Bedding & Cover', color: '#228B22' },
        75: { name: 'Cedar Thicket', category: 'Bedding & Cover', color: '#228B22' },
        76: { name: 'Leeward Slope', category: 'Bedding & Cover', color: '#228B22' },
        77: { name: 'Edge Cover', category: 'Bedding & Cover', color: '#228B22' },
        78: { name: 'Isolated Cover', category: 'Bedding & Cover', color: '#228B22' },
        79: { name: 'Man-made Cover', category: 'Bedding & Cover', color: '#228B22' },

        // Other
        99: { name: 'Other', category: 'Other', color: '#9370DB' }
    };

    // Category order for display
    const categoryOrder = ['Topographical', 'Food Resources', 'Water Resources', 'Bedding & Cover', 'Other'];

    return {
        getFeatureName: function(classificationType) {
            const config = featureConfig[classificationType];
            return config ? config.name : 'Unknown';
        },

        getFeatureColor: function(classificationType) {
            const config = featureConfig[classificationType];
            return config ? config.color : '#999999';
        },

        getFeatureCategory: function(classificationType) {
            const config = featureConfig[classificationType];
            return config ? config.category : 'Other';
        },

        getFeaturesByCategory: function() {
            const byCategory = {};
            
            // Initialize categories
            categoryOrder.forEach(category => {
                byCategory[category] = [];
            });

            // Group features by category
            Object.keys(featureConfig).forEach(typeId => {
                const config = featureConfig[typeId];
                if (byCategory[config.category]) {
                    byCategory[config.category].push({
                        id: parseInt(typeId),
                        name: config.name
                    });
                }
            });

            return byCategory;
        },

        generateFeatureOptionsHtml: function(selectedType = null) {
            const featuresByCategory = this.getFeaturesByCategory();
            let html = '';

            categoryOrder.forEach(category => {
                if (featuresByCategory[category] && featuresByCategory[category].length > 0) {
                    html += `<optgroup label="${category}">`;
                    featuresByCategory[category].forEach(feature => {
                        const selected = selectedType === feature.id ? 'selected' : '';
                        html += `<option value="${feature.id}" ${selected}>${feature.name}</option>`;
                    });
                    html += '</optgroup>';
                }
            });

            return html;
        },

        getCategoryColor: function(category) {
            const categoryColors = {
                'Topographical': '#8B4513',
                'Food Resources': '#32CD32',
                'Water Resources': '#1E90FF',
                'Bedding & Cover': '#228B22',
                'Other': '#9370DB'
            };
            return categoryColors[category] || '#999999';
        }
    };
})();