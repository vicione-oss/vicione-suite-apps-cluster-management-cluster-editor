(function(window) {
    const ViciOne = window.ViciOne ?? {};
    const Diagram = ViciOne.Diagram ?? {};

    /**
     * Helper function for BlockNodes from Z.Blazor.Diagrams
     * @class
     * @memberof ViciOne.Diagram
     */
    const BlockNode = function() {
    };

    /**
     * @type {string}
     */
    BlockNode.MEASURE_ELEMENT_ID = 'cluster-editor-block-name-measure';

    /**
     * @param {number} blockWidth
     * @param {number} defaultDiagramGridSize
     * @returns {HTMLElement}
     */
    BlockNode._createMeasureElement = function (blockWidth, defaultDiagramGridSize) {
        nameField = document.createElement('div');
        nameField.id = BlockNode.MEASURE_ELEMENT_ID;
        nameField.style.setProperty('position', 'absolute');
        nameField.style.setProperty('top', 0);
        nameField.style.setProperty('left', 0);
        nameField.style.setProperty('width', `${blockWidth}px`);
        nameField.style.setProperty('visibility', 'hidden');
        nameField.style.setProperty('font-family', 'Helvetica Neue, Segoe UI, helvetica, verdana, sans-serif');
        nameField.style.setProperty('font-size', `${1.4 * defaultDiagramGridSize}px`);
        nameField.style.setProperty('line-height', `${2 * defaultDiagramGridSize}px`);
        nameField.style.setProperty('text-align', 'center');

        return document.body.appendChild(nameField);
    }

    /**
     * @param {string[]} functionBlockNames
     * @param {number} blockWidth
     * @param {number} defaultDiagramGridSize
     * @returns {number[]}
     */
    BlockNode.measureNameFieldHeights = function (functionBlockNames, blockWidth, defaultDiagramGridSize) {
        const nameField = document.getElementById(BlockNode.MEASURE_ELEMENT_ID)
            ?? BlockNode._createMeasureElement(blockWidth, defaultDiagramGridSize);

        return functionBlockNames.map(name => {
            nameField.textContent = name;
            return Math.ceil(nameField.getBoundingClientRect().height / defaultDiagramGridSize) * defaultDiagramGridSize;
        });
    };

    Diagram.BlockNode = BlockNode;
    ViciOne.Diagram = Diagram;
    window.ViciOne = ViciOne;
})(self);
