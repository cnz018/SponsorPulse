// Blazor interactivity enhancement for SSR components
export function setupInteractivity() {
    // Enhanced button click handling
    document.addEventListener('click', async function (event) {
        const target = event.target.closest('[data-blazor-click]');
        if (target) {
            event.preventDefault();
            const handlerName = target.getAttribute('data-blazor-click');
            // Button feedback
            target.disabled = true;
            target.style.opacity = '0.6';
            setTimeout(() => {
                target.disabled = false;
                target.style.opacity = '1';
            }, 300);
        }
    });

    // Form submission enhancement
    document.addEventListener('submit', async function (event) {
        const form = event.target;
        if (form.hasAttribute('data-blazor-form')) {
            const submitBtn = form.querySelector('button[type="submit"]');
            if (submitBtn) {
                submitBtn.disabled = true;
                submitBtn.textContent = submitBtn.getAttribute('data-loading-text') || 'Traitement...';
            }
        }
    });
}

export function initializeModal(modalId) {
    const modal = document.getElementById(modalId);
    if (modal) {
        // Add close handlers for modal
        const closeButtons = modal.querySelectorAll('[data-close-modal]');
        closeButtons.forEach(btn => {
            btn.addEventListener('click', () => {
                modal.style.display = 'none';
                modal.classList.remove('show');
            });
        });
    }
}

export function showModal(modalId) {
    const modal = document.getElementById(modalId);
    if (modal) {
        modal.style.display = 'flex';
        modal.classList.add('show');
    }
}

export function hideModal(modalId) {
    const modal = document.getElementById(modalId);
    if (modal) {
        modal.style.display = 'none';
        modal.classList.remove('show');
    }
}
