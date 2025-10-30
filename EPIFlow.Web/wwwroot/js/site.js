document.addEventListener('DOMContentLoaded', () => {
    handleConfirmationForms();
    initializeDeliveryItemsEditor();
    initializeSidebarToggle();
});

function handleConfirmationForms() {
    document.querySelectorAll('form[data-confirm]').forEach(form => {
        form.addEventListener('submit', event => {
            const message = form.getAttribute('data-confirm');
            if (!message) {
                return;
            }

            event.preventDefault();

            Swal.fire({
                title: 'Confirmação',
                text: message,
                icon: 'warning',
                showCancelButton: true,
                confirmButtonColor: '#3085d6',
                cancelButtonColor: '#d33',
                confirmButtonText: 'Sim, continuar',
                cancelButtonText: 'Cancelar'
            }).then(result => {
                if (result.isConfirmed) {
                    form.submit();
                }
            });
        });
    });
}

function initializeDeliveryItemsEditor() {
    const addItemButton = document.querySelector('[data-add-delivery-item]');
    const itemsContainer = document.querySelector('[data-delivery-items-container]');
    const templateElement = document.getElementById('delivery-item-template');

    if (!addItemButton || !itemsContainer || !templateElement) {
        return;
    }

    addItemButton.addEventListener('click', event => {
        event.preventDefault();
        const index = itemsContainer.children.length;
        const templateHtml = templateElement.innerHTML.replace(/__index__/g, index);
        const tempDiv = document.createElement('div');
        tempDiv.innerHTML = templateHtml.trim();
        const newItem = tempDiv.firstChild;
        itemsContainer.appendChild(newItem);
        updateRemoveButtons(itemsContainer);
    });

    itemsContainer.addEventListener('click', event => {
        const button = event.target.closest('[data-remove-delivery-item]');
        if (!button) {
            return;
        }

        event.preventDefault();
        const item = button.closest('[data-delivery-item]');
        if (item) {
            item.remove();
            updateItemIndexes(itemsContainer);
            updateRemoveButtons(itemsContainer);
        }
    });

    updateRemoveButtons(itemsContainer);
}

function updateItemIndexes(container) {
    Array.from(container.children).forEach((element, index) => {
        element.querySelectorAll('input, select').forEach(input => {
            if (input.name) {
                input.name = input.name.replace(/\[\d+\]/, `[${index}]`);
            }
            if (input.id) {
                input.id = input.id.replace(/_(\d+)_/, `_${index}_`);
            }
        });
        element.querySelectorAll('label').forEach(label => {
            if (label.htmlFor) {
                label.htmlFor = label.htmlFor.replace(/_(\d+)_/, `_${index}_`);
            }
        });
    });
}

function updateRemoveButtons(container) {
    const items = container.querySelectorAll('[data-delivery-item]');
    items.forEach((item, idx) => {
        const button = item.querySelector('[data-remove-delivery-item]');
        if (!button) {
            return;
        }

        button.classList.toggle('d-none', items.length === 1);
        button.disabled = items.length === 1;
        button.setAttribute('aria-disabled', items.length === 1 ? 'true' : 'false');
    });
}

function initializeSidebarToggle() {
    const toggleButton = document.querySelector('[data-toggle-sidebar]');
    if (!toggleButton) {
        return;
    }

    toggleButton.addEventListener('click', event => {
        event.preventDefault();
        if (window.innerWidth < 992) {
            document.body.classList.toggle('sidebar-open');
        } else {
            document.body.classList.toggle('sidebar-collapsed');
        }
    });

    window.addEventListener('resize', () => {
        if (window.innerWidth >= 992) {
            document.body.classList.remove('sidebar-open');
        }
    });
}
