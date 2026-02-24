(function (window) {
    const ViciOne = window.ViciOne ?? {};

    /**
     * @class
     * @memberof ViciOne
     */
    const Culture = function () { };

    Culture.CurrentCulture = 'CurrentCulture';
    Culture.Empty = '';

    /**
     * @returns {string}
     */
    Culture.getCurrentCulture = function () {
        const culture = localStorage.getItem(Culture.CurrentCulture);
        return (culture) ? culture : Culture.Empty;
    }

    /**
     * @param {string} cultureName
     */
    Culture.setCurrentCulture = function (cultureName) {
        localStorage.setItem(Culture.CurrentCulture, cultureName);
    }

    ViciOne.Culture = Culture;
    window.ViciOne = ViciOne;
})(self);
