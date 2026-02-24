(function(window) {
    const ViciOne = window.ViciOne ?? {};
    const Diagram = ViciOne.Diagram ?? {};

    /**
     * Sammlung von Hilfsfunktionen für das Arbeiten mit BlockNodes von Z.Blazor.Diagrams
     * @class
     * @memberof ViciOne.Diagram
     */
    const BlockNode = function() {
    };

    /**
     * Berechnet die Höhe des Namensfeldes
     * @param {string} name
     * @param {int} blockWidth
     * @param {int} defaultDiagramGridSize
     * @returns {int}
     */
    BlockNode.measureNameFieldHeight = function (name, blockWidth, defaultDiagramGridSize) {
        const nameField = document.createElement('div');
        nameField.style.position = "absolute";
        nameField.style.visibility = "hidden";
        nameField.style.width = blockWidth + "px";
        nameField.style.lineHeight = (2 * defaultDiagramGridSize) + "px";
        nameField.style.overflow = "hidden";
        nameField.style.textAlign = "center";
        nameField.style.textOverflow = "ellipsis";
        nameField.style.fontSize = (defaultDiagramGridSize * 1.4) + "px";
        nameField.style.fontFamily = "Helvetica Neue, Segoe UI, helvetica, verdana, sans-serif";

        nameField.innerHTML = name;

        document.body.appendChild(nameField);

        const nameFieldHeight = nameField.getBoundingClientRect().height; // exakte Höhe in CSS-px
        const rasteredFieldHeight = Math.ceil(nameFieldHeight / defaultDiagramGridSize) * defaultDiagramGridSize; // auf Rastergröße gerundet

        nameField.remove();
        return rasteredFieldHeight;
    };

    Diagram.BlockNode = BlockNode;
    ViciOne.Diagram = Diagram;
    window.ViciOne = ViciOne;
})(self);
