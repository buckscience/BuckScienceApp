// SPA-style sidebar loader: injects server-rendered views into #sidebar-content
// without reloading the map or the page shell.

window.App = window.App || {};

(function () {
    const routeMap = {
        // Map your mini-navbar hash links to real endpoints here
        '#properties': '/properties',
        '#account': '/account',
        '#settings': 'account/settings'
    };

    function isHtml(str) {
        return /<\/?[a-z][\s\S]*>/i.test(str);
    }

    async function fetchPartial(url) {
        const resp = await fetch(url, {
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        });
        if (!resp.ok) throw new Error(`Failed to load ${url}: ${resp.status}`);
        return await resp.text();
    }

    async function loadSidebar(url, { push = false } = {}) {
        const container = document.getElementById('sidebar-content');
        if (!container) return;

        // Visual hint
        container.style.opacity = '0.6';

        try {
            const html = await fetchPartial(url);
            // Accept either plain fragment or full page; if full page,
            // try to extract just the #sidebar-content child
            if (isHtml(html)) {
                // naive insert: treat response as partial
                container.innerHTML = html;
            } else {
                container.textContent = html;
            }

            // Inform any feature-specific scripts
            const ev = new CustomEvent('sidebar:loaded', { detail: { url } });
            document.dispatchEvent(ev);

            if (push) {
                history.pushState({ url }, '', url);
            }

            // Update toggle position after content load with improved timing
            setTimeout(() => {
                if (window.updateTogglePosition) {
                    updateTogglePositionDebounced(true);
                }
            }, 100); // Increased from 50ms to 100ms for better reliability
        } catch (err) {
            console.error(err);
            container.innerHTML = `<div class="alert alert-danger">Failed to load content.</div>`;
        } finally {
            container.style.opacity = '';
        }
    }

    function wireRouting() {
        // Intercept navbar links that we’ve mapped
        document.body.addEventListener('click', (e) => {
            const a = e.target.closest('a');
            if (!a) return;
            const href = a.getAttribute('href');
            if (!href) return;

            // From mini navbar: #properties, #account, etc.
            if (routeMap[href]) {
                e.preventDefault();
                loadSidebar(routeMap[href], { push: true });
                return;
            }

            // Also support direct in-sidebar navigation via links marked data-sidebar-nav
            if (a.matches('[data-sidebar-nav]')) {
                e.preventDefault();
                loadSidebar(href, { push: true });
            }
        });

        // Support back/forward
        window.addEventListener('popstate', (e) => {
            const url = e.state?.url || location.pathname;
            if (url) loadSidebar(url, { push: false });
        });
    }

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

    document.addEventListener('DOMContentLoaded', () => {
        wireRouting();
        wireSidebarToggle();

        // On first load, if current URL maps to a sidebar endpoint, load it.
        // Otherwise leave whatever the server rendered by default.
        const initialHash = location.hash;
        if (routeMap[initialHash]) {
            loadSidebar(routeMap[initialHash], { push: false });
        }
    });

    // Expose for other modules
    window.App.loadSidebar = loadSidebar;
    window.updateTogglePosition = updateTogglePosition;
})();