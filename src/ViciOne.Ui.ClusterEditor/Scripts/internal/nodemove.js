(function (window) {
    const ViciOne = window.ViciOne ?? {};

    /**
     * @class
     * @memberof ViciOne
     */
    const NodeMove = function () {
    };

    /**
     * @type {number}
     */
    NodeMove._diagramZoom = 1;

    /**
     * @type {[number, number][]}
     */
    NodeMove._htmlElementPositions = [];

    /**
     * @type {NodeList}
     */
    NodeMove._htmlElements = [];

    /**
     * @type {boolean}
     */
    NodeMove._firstMove = false;

    /**
     * @type {number}
     */
    NodeMove._gridSize = 10;

    /**
     * @@type {PointerEvent}
     */
    NodeMove._lastMoveEvent = null;

    /**
     * @type {Array.<{id: string, sourceId: string, sourcePos: [number, number], targetId: string, targetPos: [number, number]}>}
     */
    NodeMove._linkData = [];

    /**
     * @type {Map}
     */
    NodeMove._linkEleMap = null;

    /**
     * @type {Map}
     */
    NodeMove._linkNodeEleMap = null;

    /**
     * @type {?number}
     */
    NodeMove._moveFrameId = null;

    /**
     * @type {object} - DotNetObjectReference
     */
    NodeMove._netObjRef = null;

    /**
     * @type {[number, number][]}
     */
    NodeMove._svgElementPositions = [];

    /**
     * @type {NodeList}
     */
    NodeMove._svgElements = [];


    /**
     * @param {number} clientX
     * @param {number} clientY
     */
    NodeMove._end = function (clientX, clientY) {
        cancelAnimationFrame(NodeMove._moveFrameId);

        document.removeEventListener('pointermove', NodeMove._move);
        document.removeEventListener('pointerup', NodeMove._up);

        NodeMove._netObjRef.invokeMethodAsync('JsMoveEndAsync', clientX, clientY);
    };

    /**
     * @param {number} clientX
     * @param {number} clientY
     */
    NodeMove._handleMove = function (clientX, clientY) {
        const pointerPos = [
            (clientX - NodeMove._containerPos[0] - NodeMove._currentPan[0]) / NodeMove._diagramZoom,
            (clientY - NodeMove._containerPos[1] - NodeMove._currentPan[1]) / NodeMove._diagramZoom];
        const currentDelta = [pointerPos[0] - NodeMove._initialCursorPos[0], pointerPos[1] - NodeMove._initialCursorPos[1]];

        for (let i = 0; i < NodeMove._htmlElements.length; i++) {
            const node = NodeMove._htmlElements[i];
            const pos = NodeMove._htmlElementPositions[i];

            const newPos = [pos[0] + currentDelta[0], pos[1] + currentDelta[1]];
            const gridPos = [
                Math.floor(newPos[0] / NodeMove._gridSize) * NodeMove._gridSize,
                Math.floor(newPos[1] / NodeMove._gridSize) * NodeMove._gridSize];

            if (gridPos[0] != pos[0] || gridPos[1] != pos[1]) {
                node.style.left = `${gridPos[0]}px`;
                node.style.top = `${gridPos[1]}px`;
            }
        }

        for (let i = 0; i < NodeMove._svgElements.length; i++) {
            const node = NodeMove._svgElements[i];
            const pos = NodeMove._svgElementPositions[i];

            const newPos = [pos[0] + currentDelta[0], pos[1] + currentDelta[1]];
            const gridPos = [
                Math.floor(newPos[0] / NodeMove._gridSize) * NodeMove._gridSize,
                Math.floor(newPos[1] / NodeMove._gridSize) * NodeMove._gridSize];

            if (gridPos[0] != pos[0] || gridPos[1] != pos[1]) {
                node.setAttribute('transform', `translate(${gridPos[0]} ${gridPos[1]})`);
            }
        }

        if (NodeMove._firstMove) {
            NodeMove._netObjRef.invokeMethodAsync('JsFirstMove');
            NodeMove._firstMove = false;
        }

        NodeMove._updateLinks();
    };

    /**
     * @param {PointerEvent} e
     */
    NodeMove._move = function (e) {
        NodeMove._lastMoveEvent = e;

        if (!NodeMove._moveFrameId) {
            NodeMove._moveFrameId = requestAnimationFrame(() => {
                NodeMove._handleMove(NodeMove._lastMoveEvent.clientX, NodeMove._lastMoveEvent.clientY);
                NodeMove._moveFrameId = null;
            });
        }
    };

    /**
     * @param {PointerEvent} e
     */
    NodeMove._up = function (e) {
        NodeMove._end(e.clientX, e.clientY);
    };

    NodeMove._updateLinks = function () {
        for (let i = 0; i < NodeMove._linkData.length; i++) {            
            const link = NodeMove._linkData[i];
            const linkEle = NodeMove._linkEleMap.get(link.id);
            const sourceEle = NodeMove._linkNodeEleMap.get(link.sourceId);
            const targetEle = NodeMove._linkNodeEleMap.get(link.targetId);

            const sx = parseInt(sourceEle.style.left, 10) + link.sourcePos[0];
            const sy = parseInt(sourceEle.style.top, 10) + link.sourcePos[1];
            const tx = parseInt(targetEle.style.left, 10) + link.targetPos[0];
            const ty = parseInt(targetEle.style.top, 10) + link.targetPos[1];

            const cx = (sx + tx) / 2;
            const cy = (sy + ty) / 2;

            // We use a simplified model for port alignment here:
            // Our blocks only support left (input) or right (output) alignment, so we assume that a port X position
            // of < 50 indicates left alignment, otherwise we assume right alignment
            const sourceMargin = Math.min(125, Math.hypot(sx - cx, sy - cy));
            const cpA = link.sourcePos[0] < 50
                ? [Math.min(sx - sourceMargin, cx), sy]
                : [Math.max(sx + sourceMargin, cx), sy];

            const targetMargin = Math.min(125, Math.hypot(tx - cx, ty - cy));
            const cpB = link.targetPos[0] < 50
                ? [Math.min(tx - targetMargin, cx), ty]
                : [Math.max(tx + targetMargin, cx), ty];

            linkEle.children[0].setAttribute('d', `M ${sx} ${sy} C ${cpA[0]} ${cpA[1]} ${cpB[0]} ${cpB[1]} ${tx} ${ty}`);
            linkEle.children[2].setAttribute('transform', `translate(${sx}, ${sy}) rotate(180)`);
            linkEle.children[3].setAttribute('transform', `translate(${tx}, ${ty}) rotate(0)`);
        }
    };

    /**
     * @param {number} panX
     * @param {number} panY
     */
    NodeMove.diagramPanChanged = function (panX, panY) {
        NodeMove._currentPan = [panX, panY];
        NodeMove._move(NodeMove._lastMoveEvent);
    };

    /**
     * @param {number} zoomValue
     */
    NodeMove.diagramZoomChanged = function (zoomValue) {
        NodeMove._diagramZoom = zoomValue;
    };

    NodeMove.reset = function () {
        NodeMove._netObjRef = null;
        NodeMove._htmlElements = [];
        NodeMove._htmlElementPositions = [];
        NodeMove._svgElements = [];
        NodeMove._svgElementPositions = [];
        NodeMove._firstMove = false;
        NodeMove._lastMoveEvent = null;
        NodeMove._linkData = [];
        NodeMove._linkEleMap = null;
        NodeMove._linkNodeEleMap = null;
        NodeMove._moveFrameId = null;
    };

    /**
     * @param {string[]} linkIds
     * @param {string[]} linkSourceIds
     * @param {double[]} linkSourcePosXs
     * @param {double[]} linkSourcePosYs
     * @param {string[]} linkTargetIds
     * @param {double[]} linkTargetPosXs
     * @param {double[]} linkTargetPosYs
     */
    NodeMove.setLinkData = function (linkIds,
        linkSourceIds,
        linkSourcePosXs,
        linkSourcePosYs,
        linkTargetIds,
        linkTargetPosXs,
        linkTargetPosYs) {

        NodeMove._linkData = [];
        for (let i = 0; i < linkIds.length; i++) {
            NodeMove._linkData.push({
                id: linkIds[i],
                sourceId: linkSourceIds[i],
                sourcePos: [linkSourcePosXs[i], linkSourcePosYs[i]],
                targetId: linkTargetIds[i],
                targetPos: [linkTargetPosXs[i], linkTargetPosYs[i]]
            });
        }

        NodeMove._linkEleMap = new Map();
        [...document.querySelectorAll('.diagram-svg-layer .diagram-link')]
            .filter(e => linkIds.includes(e.dataset.linkId))
            .forEach(e => NodeMove._linkEleMap.set(e.dataset.linkId, e));

        NodeMove._linkNodeEleMap = new Map();
        [...document.querySelectorAll('.diagram-node')]
            .filter(e => linkSourceIds.includes(e.dataset.nodeId) || linkTargetIds.includes(e.dataset.nodeId))
            .forEach(e => NodeMove._linkNodeEleMap.set(e.dataset.nodeId, e));
    };

    /**
     * @param {object} netObjRef - DotNetObjectReference
     * @param {number} gridSize
     * @param {string[]} dataIds
     * @param {number} diagramZoom
     * @param {[number, number]} containerPos
     * @param {[number, number]} pan
     * @param {[number, number]} cursorPos
     */
    NodeMove.start = function (netObjRef, gridSize, dataIds, diagramZoom, containerPos, pan, cursorPos) {
        NodeMove._netObjRef = netObjRef;

        NodeMove._htmlElements = [...document.querySelectorAll('.diagram-html-layer .diagram-node')].filter(e => dataIds.includes(e.dataset.nodeId));
        NodeMove._htmlElementPositions = NodeMove._htmlElements.map(e => [parseInt(e.style.left, 10), parseInt(e.style.top, 10)]);

        NodeMove._svgElements = [...document.querySelectorAll('.diagram-svg-layer .diagram-node')].filter(e => dataIds.includes(e.dataset.nodeId));
        NodeMove._svgElementPositions = NodeMove._svgElements.map(e => {
            const ctm = e.getCTM();
            return [ctm.e, ctm.f];
        });

        NodeMove._gridSize = gridSize;
        NodeMove._diagramZoom = diagramZoom;
        NodeMove._containerPos = containerPos;
        NodeMove._currentPan = pan;
        NodeMove._initialCursorPos = cursorPos;

        NodeMove._firstMove = true;

        document.addEventListener('pointermove', NodeMove._move);
        document.addEventListener('pointerup', NodeMove._up);
    };

    ViciOne.NodeMove = NodeMove;
    window.ViciOne = ViciOne;
})(self);
