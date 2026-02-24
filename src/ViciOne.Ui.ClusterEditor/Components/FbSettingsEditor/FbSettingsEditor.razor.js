let _dotNetHandler = null;

function handleKeyDown(e) {
    if (e.key === 'Escape' && _dotNetHandler) {
        _dotNetHandler.invokeMethodAsync('OnEscCapturedAsync');
    }
};

export function addEscEventListener(dotNetHandler) {
    _dotNetHandler = dotNetHandler;
    document.addEventListener('keydown', handleKeyDown, true);
};

export function removeEscEventListener() {
    document.removeEventListener('keydown', handleKeyDown, true);
};
