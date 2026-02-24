(function(window) {
    const ViciOne = window.ViciOne ?? {};

    /**
     * @class
     * @memberof ViciOne
     */
    const InputEvents = function() {
    };

    /**
     * @param {object} inputEventServiceRef - DotNetObjectReference<InputEventService>
     */
    InputEvents.initializeKeyboardListener = function(inputEventServiceRef) {

        window.document.addEventListener('keydown', e => {
            if (e.code == 'AltLeft')
                e.preventDefault();

            const serializedEvent = {
                key: e.key,
                code: e.code,
                location: e.location,
                repeat: e.repeat,
                ctrlKey: e.ctrlKey,
                shiftKey: e.shiftKey,
                altKey: e.altKey,
                metaKey: e.metaKey,
                type: e.type
            };

            inputEventServiceRef.invokeMethodAsync('InvokeKeyDown', serializedEvent);
        });
    };

    ViciOne.InputEvents = InputEvents;
    window.ViciOne = ViciOne;
})(self);
