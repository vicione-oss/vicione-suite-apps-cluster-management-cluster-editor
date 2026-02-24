(function(window) {
    const ViciOne = window.ViciOne ?? {};
    const Diagram = ViciOne.Diagram ?? {};

    if (!ViciOne.Bezier)
        throw 'Cannot find dependency bezier.js';

    /**
     * Sammlung von Hilfsfunktionen für das Arbeiten mit Links von Z.Blazor.Diagrams
     * Es wird davon ausgegangen, dass Links vom Typ "Smooth" bzw. kubische Bézierkurven verwendet werden
     * @class
     * @memberof ViciOne.Diagram
     */
    const Link = function() {
    };

    /**
     * Prüft, ob die Kurve sich (teilweise) in dem Rechteck befindet
     * @param {Bezier} curve
     * @param {{bottom: number, left: number, right: number, top: number}} rect
     * @returns {boolean}
     */
    Link._curveInRect = function(curve, rect) {
        const lines = [
            { p1: { x: rect.left, y: rect.top }, p2: { x: rect.left, y: rect.bottom } },
            { p1: { x: rect.left, y: rect.bottom }, p2: { x: rect.right, y: rect.bottom } },
            { p1: { x: rect.right, y: rect.bottom }, p2: { x: rect.right, y: rect.top } },
            { p1: { x: rect.right, y: rect.top }, p2: { x: rect.left, y: rect.top } }
        ];

        // Fall Kurve komplett innerhalb des Rechtecks
        const s = curve.points[0];
        const e = curve.points[3];
        if (this._pointInRect(s.x, s.y, rect) && this._pointInRect(e.x, e.y, rect))
            return true;

        // Fall Kurve teilweise innerhalb des Rechtecks
        for (let line of lines)
            if (curve.intersects(line).length)
                return true;

        return false;
    };

    /**
     * Gibt die Punkte der Bézierkurve des übergebenen Linkelements zurück
     * @param {SVGElement} linkElement - SVG g Element
     * @returns {Array<number>} - Startpunkt X/Y, Kontrollpunkt 1 X/Y, Kontrollpunkt 2 X/Y, Endpunkt X/Y
     */
    Link._getLinkCurvePoints = function(linkElement) {
        const d = linkElement.children[0].getAttribute('d');
        const points = d.split(' ').map(parseFloat);
        return [points[1], points[2], points[4], points[5], points[6], points[7], points[8], points[9]];
    };

    /**
     * Prüft, ob ein Punkt sich innerhalb eines Rechtecks befindet
     * @param {number} pointX
     * @param {number} pointY
     * @param {{bottom: number, left: number, right: number, top: number}} rect
     * @returns {boolean}
     */
    Link._pointInRect = function(pointX, pointY, rect) {
        return pointX > rect.left && pointX < rect.right && pointY > rect.top && pointY < rect.bottom;
    }

    /**
     * Gibt die 'data-link-id's von allen Links zurück, die sich (teilweise) innerhalb des übergebenen Rectangles befinden
     * @param {{bottom: number, left: number, right: number, top: number}} rectangle
     * @returns {Array<string>}
     */
    Link.getIdsInRectangle = function(rectangle) {
        const links = Array.from(window.document.querySelectorAll('.diagram-svg-layer .diagram-link'));
        const linkData = links
            .filter(l => l.childElementCount > 0)
            .map(l => ({
                curve: new ViciOne.Bezier(this._getLinkCurvePoints(l)),
                id: l.attributes['data-link-id'].value
            }));

        return linkData.reduce((acc, cur) => this._curveInRect(cur.curve, rectangle) ? acc.concat(cur.id) : acc, []);
    };

    /**
     * Projiziert den übergebenen Punkt auf den naheliegensten Punkt der Kurve und gibt den Anteil der Gesamtstrecke
     * der Kurve zurück, wobei Startpunkt S = 0, Endpunkt E = 1 ist
     * @param {number} curveSX - X Koordinate des Startpunkts der Kurve
     * @param {number} curveSY - Y Koordinate des Startpunkts der Kurve
     * @param {number} curveP1X - X Koordinate des ersten Kontrollpunkts der Kurve
     * @param {number} curveP1Y - Y Koordinate des ersten Kontrollpunkts der Kurve
     * @param {number} curveP2X - X Koordinate des zweiten Kontrollpunkts der Kurve
     * @param {number} curveP2Y - Y Koordinate des zweiten Kontrollpunkts der Kurve
     * @param {number} curveEX - X Koordinate des Endpunkts der Kurve
     * @param {number} curveEY - Y Koordinate des Endpunkts der Kurve
     * @param {number} pointX
     * @param {number} pointY
     * @returns {number} - Im Bereich [0, 1]
     */
    Link.getRatio = function(curveSX, curveSY, curveP1X, curveP1Y, curveP2X, curveP2Y, curveEX, curveEY, pointX, pointY) {
        const curve = new ViciOne.Bezier(curveSX, curveSY, curveP1X, curveP1Y, curveP2X, curveP2Y, curveEX, curveEY);
        const projectedPoint = curve.project({ x: pointX, y: pointY });
        return projectedPoint.t;
    };

    Diagram.Link = Link;
    ViciOne.Diagram = Diagram;
    window.ViciOne = ViciOne;
})(self);
