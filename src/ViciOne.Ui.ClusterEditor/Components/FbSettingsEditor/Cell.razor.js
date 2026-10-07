// The cells whose edit Escape has ended. The table hands the focus back to the cell on that key-up, before the
// key-up has reached Blazor, so the cell is told of the focus-out first and of the key that discards the edit second.
const cancelledEdits = new WeakSet();

// A focus that stays inside the cell leaves the edit open, and one that Escape sent away has nothing to commit.
export function shouldCommitEdit(container) {
    return !container.contains(document.activeElement) && !cancelledEdits.has(container);
}

function stopEscapePropagation(event) {
    if (event.key === 'Escape') {
        event.stopPropagation();
    }
}

function markEditCancelled(event) {
    if (event.key === 'Escape') {
        cancelledEdits.add(event.currentTarget.parentElement);
    }
}

// Escape ends the edit, so its keydown is kept from the elements above and the dialog around the table does
// not close on the same key press. That keeps it from Blazor as well, which is why the cell acts on the keyup.
// The listeners sit on the editor's own container, which only exists for the duration of the edit.
export function handleEscapeInEditor(container) {
    const editor = container.querySelector(':scope > .input-container');

    cancelledEdits.delete(container);

    editor?.addEventListener('keydown', stopEscapePropagation);
    editor?.addEventListener('keyup', markEditCancelled);
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
