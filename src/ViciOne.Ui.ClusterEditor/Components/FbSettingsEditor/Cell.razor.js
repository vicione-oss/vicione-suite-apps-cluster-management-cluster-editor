export function isFocusInside(container) {
    return container.contains(document.activeElement);
}

export function focusFirstInput(container) {
    const focusable = container.querySelector('input, select, textarea, [contenteditable="true"]');

    if (focusable instanceof HTMLElement) {
        focusable.focus();
    }
}

// The table's keyboard position is the focused cell, so handing focus back to the enclosing cell parks the
// editor there and the arrows continue from it. Blurring the editor with nowhere to go lands on the document body.
export function focusTable(container) {
    container.closest('td')?.focus();
}
