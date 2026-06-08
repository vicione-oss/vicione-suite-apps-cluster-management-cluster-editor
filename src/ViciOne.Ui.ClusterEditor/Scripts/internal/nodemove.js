(function (window) {
    const ViciOne = window.ViciOne ?? {};

    /**
     * It is assumed that this object doesn't store any relevant state between two start() calls
     * All the values should be set at the beginning of a move
     *
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
     * @type {[number, number]}
     */
    NodeMove._initialCursorPos = [-1, -1];

    /**
     * @type {[number, number]}
     */
    NodeMove._lastMovePos = [-1, -1];

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
     * @param {PointerEvent} e
     */
    NodeMove._eventMove = function (e) {
        NodeMove._move(e.clientX, e.clientY);
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

            node.style.left = `${gridPos[0]}px`;
            node.style.top = `${gridPos[1]}px`;
        }

        for (let i = 0; i < NodeMove._svgElements.length; i++) {
            const node = NodeMove._svgElements[i];
            const pos = NodeMove._svgElementPositions[i];

            const newPos = [pos[0] + currentDelta[0], pos[1] + currentDelta[1]];
            const gridPos = [
                Math.floor(newPos[0] / NodeMove._gridSize) * NodeMove._gridSize,
                Math.floor(newPos[1] / NodeMove._gridSize) * NodeMove._gridSize];

            node.setAttribute('transform', `translate(${gridPos[0]} ${gridPos[1]})`);
        }

        if (NodeMove._firstMove) {
            NodeMove._netObjRef.invokeMethodAsync('JsFirstMove');
            NodeMove._firstMove = false;
        }

        NodeMove._updateLinks();
    };

    /**
     * @param {object} netObjRef - DotNetObjectReference
     * @param {number} gridSize
     * @param {string[]} dataIds
     * @param {number} diagramZoom
     * @param {[number, number]} containerPos
     * @param {[number, number]} pan
     * @param {[number, number]} cursorPos
     * @param {boolean} useJsDomEvents
     */
    NodeMove._init = function (netObjRef, gridSize, dataIds, diagramZoom, containerPos, pan, cursorPos, useJsDomEvents) {
        if (NodeMove._moveFrameId) {
            NodeMove.end();
        }

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
        NodeMove._lastMovePos = cursorPos;

        NodeMove._firstMove = true;

        if (useJsDomEvents) {
            document.addEventListener('pointermove', NodeMove._eventMove);
            document.addEventListener('pointerup', NodeMove._up);
        }
    };

    /**
     * @param {number} clientX
     * @param {number} clientY
     */
    NodeMove._move = function (clientX, clientY) {
        NodeMove._lastMovePos = [clientX, clientY];

        if (!NodeMove._moveFrameId) {
            NodeMove._moveFrameId = requestAnimationFrame(() => {
                NodeMove._handleMove(NodeMove._lastMovePos[0], NodeMove._lastMovePos[1]);
                NodeMove._moveFrameId = null;
            });
        }
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
    NodeMove._setLinkData = function (linkIds,
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

    NodeMove._up = function () {
        NodeMove.end();
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
        NodeMove._move(NodeMove._lastMovePos[0], NodeMove._lastMovePos[1]);
    };

    /**
     * @param {number} zoomValue
     */
    NodeMove.diagramZoomChanged = function (zoomValue) {
        NodeMove._diagramZoom = zoomValue;
    };

    NodeMove.end = function () {
        cancelAnimationFrame(NodeMove._moveFrameId);
        NodeMove._moveFrameId = null;

        document.removeEventListener('pointermove', NodeMove._eventMove);
        document.removeEventListener('pointerup', NodeMove._up);
    };

    /**
     * Allows for externally driven movement, e.g. to forward drag event coordinates.
     * This method should be used after a NodeMove.start initialization with the flag
     * useJsDomEvents = false.
     * @param {number} clientX
     * @param {number} clientY
     */
    NodeMove.externalMove = function (clientX, clientY) {
        NodeMove._move(clientX, clientY);
    };

    /**
     * @param {string[]} linkIds
     * @param {string[]} linkSourceIds
     * @param {number[]} linkSourcePosXs
     * @param {number[]} linkSourcePosYs
     * @param {string[]} linkTargetIds
     * @param {number[]} linkTargetPosXs
     * @param {number[]} linkTargetPosYs
     * @param {object} netObjRef - DotNetObjectReference
     * @param {number} gridSize
     * @param {string[]} dataIds
     * @param {number} diagramZoom
     * @param {[number, number]} containerPos
     * @param {[number, number]} pan
     * @param {[number, number]} cursorPos
     * @param {boolean} useJsDomEvents
     */
    NodeMove.start = function (linkIds, linkSourceIds, linkSourcePosXs, linkSourcePosYs, linkTargetIds, linkTargetPosXs, linkTargetPosYs,
        netObjRef, gridSize, dataIds, diagramZoom, containerPos, pan, cursorPos, useJsDomEvents) {

        NodeMove._setLinkData(linkIds,
            linkSourceIds,
            linkSourcePosXs,
            linkSourcePosYs,
            linkTargetIds,
            linkTargetPosXs,
            linkTargetPosYs);

        NodeMove._init(netObjRef, gridSize, dataIds, diagramZoom, containerPos, pan, cursorPos, useJsDomEvents);
    };

    /**
     * Waits until every id in `dataIds` has a corresponding element in either
     * `.diagram-html-layer .diagram-node` or `.diagram-svg-layer .diagram-node`.
     * Resolves true when all are present, false on timeout.
     * @param {string[]} dataIds
     * @param {number} [timeoutMs=5000]
     * @returns {Promise<boolean>}
     */
    NodeMove.waitForNodes = function (dataIds, timeoutMs = 5000) {
        if (!dataIds || dataIds.length == 0)
            return Promise.resolve(true);

        const idSet = new Set(dataIds);

        const isReady = () => {
            const hits = new Set();
            document.querySelectorAll('.diagram-html-layer .diagram-node, .diagram-svg-layer .diagram-node').forEach(e => {
                const id = e.dataset.nodeId;
                if (idSet.has(id))
                    hits.add(id);
            });
            return hits.size == idSet.size;
        };

        if (isReady())
            return Promise.resolve(true);

        return new Promise(resolve => {
            let done = false;
            const finish = (result) => {
                if (done)
                    return;

                done = true;
                observer.disconnect();
                clearTimeout(timer);
                resolve(result);
            };

            const observer = new MutationObserver(() => {
                if (isReady())
                    finish(true);
            });

            const diagramContainerEle = document.querySelector('.diagram-container');
            if (!diagramContainerEle) {
                finish(false);
            } else {
                observer.observe(diagramContainerEle, {
                    childList: true,
                    subtree: true,
                    attributes: true,
                    attributeFilter: ['data-node-id']
                });
            }

            const timer = setTimeout(() => finish(false), timeoutMs);
        });
    };

    ViciOne.NodeMove = NodeMove;
    window.ViciOne = ViciOne;
})(self);
