(function(window) {
    const ViciOne = window.ViciOne ?? {};

    /**
     * @class
     * @memberof ViciOne
     */
    const File = function() {
    };

    /**
     * @param {number} saveSlot
     */
    File.clear = function(saveSlot) {
        localStorage.removeItem(`dataflow${saveSlot}`);
    };

    /**
     * @param {string} data
     */
    File.download = function(data) {
        const dlEle = window.document.createElement('a');
        dlEle.setAttribute('href', `data:text/plain;charset=utf-8,${window.encodeURIComponent(data)}`);
        dlEle.setAttribute('download', 'dataflow.json');
        dlEle.style.setProperty('display', 'none');

        window.document.body.appendChild(dlEle);
        dlEle.click();
        window.document.body.removeChild(dlEle);
    };

    /**
     * @returns {number[]}
     */
    File.getSizes = function() {
        const result = [];
        for (let key of ['dataflow1', 'dataflow2', 'dataflow3']) {
            if (!localStorage[key])
                result.push(-1);
            else
                result.push(Math.round(new Blob([localStorage[key]]).size / 1024));
        }
        return result;
    };

    /**
     * @param {number} saveSlot
     * @returns {boolean}
     */
    File.hasValue = function(saveSlot) {
        return localStorage[`dataflow${saveSlot}`] != null;
    }

    /**
     * @param {number} saveSlot
     * @returns {string}
     */
    File.load = function(saveSlot) {
        return ViciOne.LZString.decompress(localStorage.getItem(`dataflow${saveSlot}`));
    };

    /**
     * @param {number} saveSlot
     * @param {string} dataflowJson
     * @returns {boolean}
     */
    File.save = function(saveSlot, dataflowJson) {
        try {
            localStorage.setItem(`dataflow${saveSlot}`, ViciOne.LZString.compress(dataflowJson));
        }
        catch {
            return false;
        }

        return true;
    };

    ViciOne.File = File;
    window.ViciOne = ViciOne;
})(self);
