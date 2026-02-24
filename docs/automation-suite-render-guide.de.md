# Automation Suite Render Guide

Dieses Dokument enthält Hinweise für die Darstellung von Elementen aus der aktuellen Automation Suite.

## Grid

- Quadratisch aufgebaut, Seitenlänge 10px

## Funktionsblöcke

Die Funktionsblöcke orientieren sich in der Darstellung an der Größe der Gridzellen (Grid Cells - gc).

- Gesamtbreite 16gc
- Höhe einer Row 2gc
- Aufteilung in Header und Body
  - Header
    - Row Name
      - Zentriert
    - Row NodeId / Run Mode
      - 6gc NodeId
      - 8gc Run Mode (4 x 2gc bei Cycle)
      - 2gc Schönheitskasten
    - 4 Rows Header Konnektoren und FB Bild in der Mitte
      - 2gc Input Port
      - 14gc Bild
      - 2gc Output Port
      - Bei Selektion von Input oder Output Port wird der gesamte Konnektor (Darstellung siehe Body) sichtbar und das Bild wird ausgeblendet
  - Body
    - n Rows mit Input und Output Konnektoren	
      - 2gc Input Port
      - 2gc Pooling Mode
      - 4gc Input Konnektor Name
      - 6gc Output Konnektor Name rechtbündig
      - 2gc Output Port
