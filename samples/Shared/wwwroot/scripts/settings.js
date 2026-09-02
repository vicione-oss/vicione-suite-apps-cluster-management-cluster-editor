(function (window) {
    const ViciOne = window.ViciOne ?? {};

    /**
     * @class
     * @memberof ViciOne
     */
    class Settings {
        constructor() { }

        /**
        * @returns {string}
        */
        static getCurrentSettings() {
            const settings = localStorage.getItem(Settings.Key);
            return (settings) ? settings : Settings.Empty;
        }

        /**
        * @param {string} settings
        */
        static setCurrentSettings(settings) {
            localStorage.setItem(Settings.Key, settings);
        }
    }

    Settings.Key = 'CurrentSettings';
    Settings.Empty = '';

    ViciOne.Settings = Settings;
    window.ViciOne = ViciOne;
})(self);
