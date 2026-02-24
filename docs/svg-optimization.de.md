# SVG Optimierungen

Wie in Issue [#162](https://git.nsc-gmbh.de/temp/vo-analytics-web/blazor/vo-dataflow-editor/-/issues/162) beschrieben, sollten SVG Bilder vom UI Team
optimiert werden, da die Software zum Erstellen der Bilder den SVG Code mit unnötigen und ungewünschten Informationen anreichert.

## Optimierung

Zum Optimieren wird die Web App Implementation von [SVGO](https://github.com/svg/svgo) benutzt. Diese kann unter https://jakearchibald.github.io/svgomg/
aufgerufen werden.

Es werden die Default Einstellungen mit folgenden Abweichungen genutzt:
- Prettify Markup: true
- Prefer viewBox to width/height: true

Default Einstellungen sollten sein:
- Multipass: false
- Number precision: 3
- Transform precision: 5
- Remove doctype: true
- Remove XML instructions: true
- Remove comments: true
- Remove <metadata>: true
- Remove xmlns: false
- Remove editor data: true
- Clean up attribute whitespace: true
- Merge styles: true
- Inline styles: true
- Minify styles: true
- Styles to attributes: false
- Clean up IDs: true
- Remove raster images: false
- Remove unused defs: true
- Round/rewrite numbers: true
- Round/rewrite number lists: false
- Minify colours: true
- Remove unknowns & defaults: true
- Remove unneeded group attrs: true
- Remove useless stroke & fill: true
- Remove viewBox: true
- Remove/tidy enable-background: true
- Remove hidden elements: true
- Remove empty text: true
- Shapes to (smaller) paths: true
- Move attrs to parent group: true
- Move group attrs to elements: true
- Collapse useless groups: true
- Round/rewrite paths: true
- Convert non-eccentric <ellipse> to <circle>: true
- Round/rewrite transforms: true
- Remove empty attrs: true
- Remove empty containers: true
- Merge paths: true
- Remove unused namespaces: true
- Replace duplicate elements with links: false
- Sort attrs: false
- Sort children of <defs>: true
- Remove <title>: true
- Remove <desc>: true
- Remove style elements: false
- Remove script elements: false
- Remove out-of-bounds paths: false
