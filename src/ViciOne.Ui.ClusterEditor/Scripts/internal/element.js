(function(window) {
    const ViciOne = window.ViciOne ?? {};

    /**
     * Sammlung von Hilfsfunktionen für das Arbeiten mit HTML Elementen
     * @class
     * @memberof ViciOne.Diagram
     */
    const Element = function() {
    };

    /**
     * Fokussiert das erste Element mit der uebergebenen Klasse
     * @param {string} classes
     */
    Element.focusByClass = function(classes) {
        document.getElementsByClassName(classes)[0].focus();
    }

    /**
     * @param {String} htmlString
     * @returns {HTMLElement}
     */
    Element.fromHTML = function(htmlString) {
        const template = document.createElement('template');
        template.innerHTML = htmlString.trim();
        return template.content.firstChild;
    };

    /**
     * Liefert als Ergebnis zurück ob sich das Fenster oder ein Element im Fullscreen Modus befindet
     * @param {HTMLElement} [elementRef] - Wenn nicht angegeben, wird das 'window' Element getestet
     * @returns {boolean}
     */
    Element.getFullscreenState = function(elementRef) {
        if (elementRef) {
            return (elementRef.offsetWidth == screen.width && elementRef.offsetHeight == screen.height);
        } else {
            return (window.innerWidth == screen.width && window.innerHeight == screen.height);
        }
    }

    /**
     * Selektiert den Inhalt eines Input-Elements
     * @param {string} elementId - Id des Input-Elements
     */
    Element.selectInputContent = function(elementId) {
        const inputElement = document.getElementById(elementId);
        inputElement.select();
    };

    /**
     * Setzt den Wert eines Input-Elements
     * @param {string} elementId - Id des Input-Elements
     */
    Element.setInputContent = function(elementId, value) {
        const inputElement = document.getElementById(elementId);
        inputElement.value = value;
    };

    /**
     * Versetzt die Webseite in den Fullscreen Modus
     * @param {boolean} fullscreen
     * @param {object} dotNetRef - DotNetObjectReference<>
     * @param {string} dotNetMethodName - The name of the [JSInvocable] method to call
     */
    Element.setFullscreen = function(fullscreen, dotNetRef, dotNetExitHandlerMethodName) {
        const onFullscreenChange = () => {
            if (window.document.fullscreenElement) {
                return;
            }

            window.document.removeEventListener('fullscreenchange', onFullscreenChange);

            if (dotNetRef) {
                dotNetRef.invokeMethodAsync(dotNetExitHandlerMethodName);
            }
        };

        if (fullscreen) {
            window.document.body.requestFullscreen();
            window.document.addEventListener('fullscreenchange', onFullscreenChange);
        } else if (document.fullscreenElement) {
            document.exitFullscreen();
        }
    };

    ViciOne.Element = Element;
    window.ViciOne = ViciOne;
})(self);
