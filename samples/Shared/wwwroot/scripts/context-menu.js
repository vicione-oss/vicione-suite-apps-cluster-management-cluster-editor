(function (window) {
    const ViciOne = window.ViciOne ?? {};

    /**
     * @class
     * @memberof ViciOne
     */
    class ContextMenu {
        constructor() { }

        /**
        * Suppresses the default browser context menu while it is disabled in the current settings.
        * Registered in the capture phase on the window so that it cannot be bypassed by any
        * stopPropagation() further down the tree.
        * @param {MouseEvent} event
        */
        static onContextMenu(event) {
            if (!ContextMenu.ShowDefault && event.cancelable)
                event.preventDefault();
        }

        /**
        * @param {boolean} showDefaultContextMenu
        */
        static register(showDefaultContextMenu) {
            ContextMenu.setShowDefaultContextMenu(showDefaultContextMenu);

            if (ContextMenu.Registered)
                return;

            window.addEventListener('contextmenu', ContextMenu.onContextMenu, { capture: true });
            ContextMenu.Registered = true;
        }

        /**
        * @param {boolean} showDefaultContextMenu
        */
        static setShowDefaultContextMenu(showDefaultContextMenu) {
            ContextMenu.ShowDefault = showDefaultContextMenu !== false;
        }

        static unregister() {
            if (!ContextMenu.Registered)
                return;

            window.removeEventListener('contextmenu', ContextMenu.onContextMenu, { capture: true });
            ContextMenu.Registered = false;
        }
    }

    ContextMenu.Registered = false;
    ContextMenu.ShowDefault = true;

    ViciOne.ContextMenu = ContextMenu;
    window.ViciOne = ViciOne;
})(self);
