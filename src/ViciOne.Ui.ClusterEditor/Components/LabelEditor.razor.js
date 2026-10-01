/**
 * @type {EasyMDE}
 */
let editor = null;

/**
 * @param {string} content
 */
function renderEditor(content) {
    const contentContainerEle = document.querySelector('.label-editor-content-container');
    const contentAreaEle = document.getElementById('label-editor-content-area');
    if (!contentContainerEle || !contentAreaEle)
        return;

    const contentHeight = contentContainerEle.getBoundingClientRect().height - 118;

    editor = new EasyMDE({
        element: contentAreaEle,
        initialValue: content,
        maxHeight: `${contentHeight}px`,
        minHeight: `${contentHeight}px`,
        sideBySideFullscreen: false,
        spellChecker: false,
        toolbar: [
            "undo", "redo", "|",
            "bold", "italic", "strikethrough", "heading", "|",
            "quote", "code", "|",
            "horizontal-rule", "unordered-list", "ordered-list", "table", "|",
            "link", "image", "|",
            "preview", "side-by-side"
        ],
        renderingConfig: {
            sanitizerFunction: (renderedHtml) =>
                DOMPurify.sanitize(renderedHtml)
        },
    });
}

/**
 * @param {string} content
 */
export function showEditor(content) {
    // This timeout is necessary to let the Dialog resize itself
    setTimeout(() => renderEditor(content), 100)
}

/**
 * @returns {string}
 */
export function getValue() {
    return DOMPurify.sanitize(editor.value());
}
