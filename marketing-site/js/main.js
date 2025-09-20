// BuckScience Marketing Website - Main JavaScript

(function() {
    'use strict';

    // Initialize when DOM is loaded
    document.addEventListener('DOMContentLoaded', function() {
        initNavbar();
        initScrollAnimations();
        initSmoothScrolling();
        initCTATracking();
    });

    // Navbar functionality
    function initNavbar() {
        const navbar = document.getElementById('navbar');
        const mobileToggle = document.querySelector('.mobile-menu-toggle');
        const navMenu = document.querySelector('.nav-menu');

        // Add scrolled class to navbar on scroll
        function handleScroll() {
            if (window.scrollY > 50) {
                navbar.classList.add('scrolled');
            } else {
                navbar.classList.remove('scrolled');
            }
        }

        // Mobile menu toggle (future enhancement)
        if (mobileToggle && navMenu) {
            mobileToggle.addEventListener('click', function() {
                navMenu.classList.toggle('mobile-open');
                this.classList.toggle('active');
            });
        }

        // Throttled scroll handler for performance
        let scrollTimeout;
        window.addEventListener('scroll', function() {
            if (!scrollTimeout) {
                scrollTimeout = setTimeout(function() {
                    handleScroll();
                    scrollTimeout = null;
                }, 10);
            }
        });

        // Initial call
        handleScroll();
    }

    // Smooth scrolling for anchor links
    function initSmoothScrolling() {
        const links = document.querySelectorAll('a[href^="#"]');
        
        links.forEach(link => {
            link.addEventListener('click', function(e) {
                const href = this.getAttribute('href');
                
                // Skip if href is just '#'
                if (href === '#') {
                    e.preventDefault();
                    return;
                }

                const target = document.querySelector(href);
                if (target) {
                    e.preventDefault();
                    
                    // Calculate offset for fixed navbar
                    const navbarHeight = document.querySelector('.navbar').offsetHeight;
                    const targetPosition = target.offsetTop - navbarHeight - 20;
                    
                    window.scrollTo({
                        top: targetPosition,
                        behavior: 'smooth'
                    });
                }
            });
        });
    }

    // Scroll animations
    function initScrollAnimations() {
        const observerOptions = {
            threshold: 0.1,
            rootMargin: '0px 0px -50px 0px'
        };

        const observer = new IntersectionObserver(function(entries) {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    entry.target.classList.add('fade-in-up');
                    // Stop observing once animated
                    observer.unobserve(entry.target);
                }
            });
        }, observerOptions);

        // Observe elements that should animate on scroll
        const animateElements = document.querySelectorAll('.card, .stat-item, .pricing-card');
        animateElements.forEach(el => {
            observer.observe(el);
        });
    }

    // CTA Button tracking (placeholder for analytics)
    function initCTATracking() {
        const ctaButtons = document.querySelectorAll('.btn-primary');
        
        ctaButtons.forEach(button => {
            button.addEventListener('click', function(e) {
                const buttonText = this.textContent.trim();
                const section = this.closest('section')?.id || 'unknown';
                
                // Placeholder for analytics tracking
                console.log('CTA Click:', {
                    button: buttonText,
                    section: section,
                    timestamp: new Date().toISOString()
                });

                // For demo purposes, show alert instead of actual signup
                if (buttonText.includes('Trial') || buttonText.includes('Get Started')) {
                    e.preventDefault();
                    showTrialModal();
                }
            });
        });
    }

    // Demo modal for trial signup (placeholder)
    function showTrialModal() {
        // Create modal overlay
        const overlay = document.createElement('div');
        overlay.style.cssText = `
            position: fixed;
            top: 0;
            left: 0;
            right: 0;
            bottom: 0;
            background: rgba(0, 0, 0, 0.8);
            display: flex;
            align-items: center;
            justify-content: center;
            z-index: 10000;
            animation: fadeInUp 0.3s ease-out;
        `;

        // Create modal content
        const modal = document.createElement('div');
        modal.style.cssText = `
            background: white;
            padding: 2rem;
            border-radius: 12px;
            max-width: 500px;
            width: 90%;
            text-align: center;
            box-shadow: 0 8px 32px rgba(0, 0, 0, 0.3);
        `;

        modal.innerHTML = `
            <h3 style="color: #36454F; margin-bottom: 1rem;">Start Your Free Trial</h3>
            <p style="margin-bottom: 1.5rem; color: #2d3748;">
                Ready to transform your hunting data into intelligence? 
                Enter your email to get started with BuckScience.
            </p>
            <form style="margin-bottom: 1.5rem;">
                <input type="email" placeholder="Enter your email" style="
                    width: 100%;
                    padding: 12px;
                    border: 2px solid #e1e4e8;
                    border-radius: 8px;
                    font-size: 16px;
                    margin-bottom: 1rem;
                " required>
                <button type="submit" style="
                    background: #527A52;
                    color: white;
                    border: none;
                    padding: 12px 24px;
                    border-radius: 8px;
                    font-weight: 600;
                    cursor: pointer;
                    width: 100%;
                    font-size: 16px;
                ">Start Free Trial</button>
            </form>
            <p style="font-size: 0.875rem; color: #6c757d; margin-bottom: 1rem;">
                No credit card required. 14-day free trial.
            </p>
            <button id="closeModal" style="
                background: transparent;
                border: none;
                color: #6c757d;
                cursor: pointer;
                text-decoration: underline;
            ">Close</button>
        `;

        overlay.appendChild(modal);
        document.body.appendChild(overlay);

        // Handle form submission
        const form = modal.querySelector('form');
        form.addEventListener('submit', function(e) {
            e.preventDefault();
            const email = this.querySelector('input[type="email"]').value;
            
            // Show success message
            modal.innerHTML = `
                <h3 style="color: #527A52; margin-bottom: 1rem;">✓ Welcome to BuckScience!</h3>
                <p style="margin-bottom: 1.5rem; color: #2d3748;">
                    Thank you for signing up! We'll send you login instructions at <strong>${email}</strong>.
                </p>
                <button id="closeModal" style="
                    background: #527A52;
                    color: white;
                    border: none;
                    padding: 12px 24px;
                    border-radius: 8px;
                    font-weight: 600;
                    cursor: pointer;
                ">Get Started</button>
            `;

            // Re-attach close handler
            modal.querySelector('#closeModal').addEventListener('click', function() {
                document.body.removeChild(overlay);
            });
        });

        // Close modal handlers
        const closeButton = modal.querySelector('#closeModal');
        closeButton.addEventListener('click', function() {
            document.body.removeChild(overlay);
        });

        overlay.addEventListener('click', function(e) {
            if (e.target === overlay) {
                document.body.removeChild(overlay);
            }
        });

        // Close on escape key
        document.addEventListener('keydown', function(e) {
            if (e.key === 'Escape') {
                if (document.body.contains(overlay)) {
                    document.body.removeChild(overlay);
                }
            }
        });
    }

    // Utility function to throttle function calls
    function throttle(func, limit) {
        let inThrottle;
        return function() {
            const args = arguments;
            const context = this;
            if (!inThrottle) {
                func.apply(context, args);
                inThrottle = true;
                setTimeout(() => inThrottle = false, limit);
            }
        };
    }

    // Initialize mobile menu functionality (future enhancement)
    function initMobileMenu() {
        // This would be expanded for a full mobile menu implementation
        // For now, we just hide the menu on mobile as indicated in CSS
        console.log('Mobile menu initialization - to be implemented');
    }

    // Navbar brand animation on load
    function animateBrand() {
        const brand = document.querySelector('.nav-brand');
        if (brand) {
            brand.style.transform = 'scale(1.1)';
            setTimeout(() => {
                brand.style.transform = 'scale(1)';
            }, 200);
        }
    }

    // Call brand animation after a short delay
    setTimeout(animateBrand, 500);

})();