// Simplified sidebar functionality - handles toggle and map layer updates only
// All navigation is now handled via standard MVC routing

window.App = window.App || {};

(function () {
    function updateTogglePosition() {
        const btn = document.getElementById('sidebar-toggle');
        const aside = document.getElementById('sidebar');
        if (!btn || !aside) return;

        // Use requestAnimationFrame to ensure positioning happens after layout
        requestAnimationFrame(() => {
            try {
                // Calculate the actual width of the sidebar
                const sidebarRect = aside.getBoundingClientRect();
                const sidebarRight = sidebarRect.right;
                
                // Position the toggle at the right edge of the sidebar
                // Don't update position when collapsed to let CSS handle it
                if (!aside.classList.contains('collapsed')) {
                    btn.style.left = sidebarRight + 'px';
                } else {
                    // Reset to let CSS handle collapsed positioning
                    btn.style.left = '';
                }
                
                // Show the button now that it's positioned correctly
                btn.classList.add('positioned');
            } catch (error) {
                console.warn('Error updating toggle position:', error);
                // Fallback: show button even if positioning failed
                btn.classList.add('positioned');
            }
        });
    }

    // Debounced version of updateTogglePosition to prevent excessive calls
    let updateTogglePositionDebounced = (() => {
        let timeoutId;
        let isInitialized = false;
        return (forceImmediate = false) => {
            clearTimeout(timeoutId);
            if (forceImmediate || isInitialized) {
                timeoutId = setTimeout(updateTogglePosition, 16); // ~60fps
            } else {
                // For initial positioning, add a longer delay to ensure layout is stable
                timeoutId = setTimeout(() => {
                    updateTogglePosition();
                    isInitialized = true;
                }, 200);
            }
        };
    })();

    function wireSidebarToggle() {
        const btn = document.getElementById('sidebar-toggle');
        const aside = document.getElementById('sidebar');
        if (!btn || !aside) return;

        // Initial positioning with longer delay for first load
        updateTogglePositionDebounced();
        
        // Update position on window resize (using immediate mode after initialization)
        window.addEventListener('resize', () => updateTogglePositionDebounced(true));

        btn.addEventListener('click', () => {
            aside.classList.toggle('collapsed');
            const icon = btn.querySelector('i');

            // Simple collapsed style
            if (aside.classList.contains('collapsed')) {
                aside.style.transform = 'translateX(-100%)';
                // Change icon to point right (to open)
                if (icon) {
                    icon.className = 'fas fa-chevron-right';
                }
            } else {
                aside.style.transform = '';
                // Change icon to point left (to close)
                if (icon) {
                    icon.className = 'fas fa-chevron-left';
                }
            }
            
            // Update toggle position after animation
            setTimeout(() => updateTogglePositionDebounced(true), 300);
        });

        // Listen for Bootstrap accordion events that might change sidebar content size
        document.addEventListener('shown.bs.collapse', () => updateTogglePositionDebounced(true));
        document.addEventListener('hidden.bs.collapse', () => updateTogglePositionDebounced(true));
        
        // Use ResizeObserver to watch for sidebar size changes (modern browsers)
        if (window.ResizeObserver) {
            const resizeObserver = new ResizeObserver(() => updateTogglePositionDebounced(true));
            resizeObserver.observe(aside);
        }
        
        // Use MutationObserver to watch for content changes that might affect sidebar width
        const mutationObserver = new MutationObserver(() => updateTogglePositionDebounced(true));
        mutationObserver.observe(aside, {
            childList: true,
            subtree: true,
            attributes: true,
            attributeFilter: ['class', 'style']
        });
    }

    // Lightweight function to trigger map layer updates when new content is loaded
    function triggerMapLayerUpdate(context = {}) {
        // Dispatch event for map layer updates
        const event = new CustomEvent('map:updateLayers', { detail: context });
        document.dispatchEvent(event);
    }

    document.addEventListener('DOMContentLoaded', () => {
        wireSidebarToggle();
        
        // Trigger initial map layer update based on current page context
        const currentPath = window.location.pathname;
        const propertyMatch = currentPath.match(/\/properties\/(\d+)/);
        const cameraMatch = currentPath.match(/\/cameras\/(\d+)/);
        
        if (propertyMatch) {
            triggerMapLayerUpdate({ propertyId: parseInt(propertyMatch[1]) });
        } else if (cameraMatch) {
            triggerMapLayerUpdate({ cameraId: parseInt(cameraMatch[1]) });
        }
    });

    // Expose utilities for other modules
    window.updateTogglePosition = updateTogglePosition;
    window.App.triggerMapLayerUpdate = triggerMapLayerUpdate;
})();