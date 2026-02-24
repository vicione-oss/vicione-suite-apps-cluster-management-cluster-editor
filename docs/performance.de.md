# Performancemessungen 06/2021

## Blazor Code

### Messmethode

1. Release Build, Solution Rebuild All
2. Startprojekt Server
3. Start Without Debugging (Ctrl + F5)
4. Browser neu öffnen
5. Seite aufrufen, sicher stellen, dass die Browserkonsole geschlossen ist
6. Auf die Demo Seite gehen, zu testentes Objekt (Sonos 50x) einmal erzeugen
7. Auf die Index Seite wechseln, zurück auf die Demo Seite wechseln
8. "Sonos 50x" anklicken, keine weiteren Aktionen durchführen bis die Blöcke angezeigt werden
9. Browserkonsole öffnen, "Write Log" klicken
10. Die zuletzt gerenderte Komponente ist der wichtige Wert, z.B. im Moment `8720 - FunctionBlockOutputConnectorComponent(ST).OnAfterRender(False)` (es kann sein, dass die Seite später nochmal neu gerendert wird, z.B. durch MouseMove Events, das sollte ignoriert werden)
11. Wert notieren, Browserkonsole leeren und schließen
12. Schritte 7-11 5 mal durchführen (öfters wenn die Werte stark voneinander abweichen)
13. Der Medianwert ist das Endergebnis

### Daten

#### Michael

| Stand   | Ergebnis | Kommentar |
| -----   | -------- | --------- |
| ebd0a7f | 8562     | Ursprünglicher Zustand |
| 9abcd58 | 7892     | Statische Luminance Regex und Caching der Ergebnisse |
| c06e889 | 6045     | PortRenderer Builder & JSInterop optimierungen |
| 014ee0d | 5298     | FunctionBlock Subkomponenten entfernen |
| 56738c2 | 4681     | Unnötige divs entfernen |
| fcf3699 | 1684     | OnResize bei erstem Durchlauf unterbinden & kleinere NodeRenderer Optimierungen |

##### Test mit Switch FB

Methode wie oben beschrieben, allerdings wird "Switch 50x" anstatt Sonos verwendet.

| Stand   | Ergebnis | Kommentar |
| -----   | -------- | --------- |
| 27277ad | 439      | OnResize bei erstem Durchlauf unterbinden & kleinere NodeRenderer Optimierungen |

#### Kevin

| Stand   | Ergebnis | Kommentar |
| -----   | -------- | --------- |
| ebd0a7f | 14700    | Ursprünglicher Zustand |
| 9abcd58 | 13475    | Statische Luminance Regex und Caching der Ergebnisse |
| c06e889 | 8752     | PortRenderer Builder & JSInterop optimierungen |
| 014ee0d | 7715     | FunctionBlock Subkomponenten entfernen |
| 56738c2 | 6849     | Unnötige divs entfernen |
| fcf3699 | 2061     | OnResize bei erstem Durchlauf unterbinden & kleinere NodeRenderer Optimierungen |

## Browser

### Messmethode

Beschreibung bezieht sich auf den Google Chrome

1. Release Build, Solution Rebuild All
2. Startprojekt Server
3. Start Without Debugging (Ctrl + F5)
4. Browser neu öffnen
5. Seite aufrufen, Browserkonsole im Tab Performance öffnen
6. Auf die Demo Seite gehen, zu testentes Objekt (Sonos 50x) einmal erzeugen
7. Auf die Index Seite wechseln, zurück auf die Demo Seite wechseln
8. Performancemessung in Browser starten (Ctrl + E), 2-3 Sekunden warten
9. "Sonos 50x" anklicken, keine weiteren Aktionen durchführen bis die Blöcke angezeigt werden
10. 2-3 Sekunden warten, Messung beenden
11. Über dem Flamechart in der Zeile Frames sollte ein langer roter Frame angezeigt werden, dieses ist die relevante Zeit
12. Wert notieren, Performancemessung löschen
13. Schritte 7-12 5 mal durchführen (öfters wenn die Werte stark voneinander abweichen)
14. Der Medianwert ist das Endergebnis

Wichtig: Diese Ergebnisse sind nicht mit den obigen Ergebnissen identisch, die Performancemessung im Browser selbst hat deutlich negativen Einfluss auf das Ergebnis.

### Daten

#### Michael

| Stand   | Ergebnis | Kommentar |
| -----   | -------- | --------- |
| ebd0a7f | 14264    | Ursprünglicher Zustand |
| 9abcd58 | 13213    | Statische Luminance Regex und Caching der Ergebnisse |
| c06e889 | 14197    | PortRenderer Builder & JSInterop optimierungen |
| 014ee0d | 12729    | FunctionBlock Subkomponenten entfernen |
| 56738c2 | 11661    | Unnötige divs entfernen |
| fcf3699 | 7674     | OnResize bei erstem Durchlauf unterbinden & kleinere NodeRenderer Optimierungen |

##### Test mit Switch FB

Methode wie oben beschrieben, allerdings wird "Switch 50x" anstatt Sonos verwendet.

| Stand   | Ergebnis | Kommentar |
| -----   | -------- | --------- |
| 27277ad | 1968     | OnResize bei erstem Durchlauf unterbinden & kleinere NodeRenderer Optimierungen |

#### Kevin

| Stand   | Ergebnis | Kommentar |
| -----   | -------- | --------- |
| ebd0a7f | 27474    | Ursprünglicher Zustand |
| 9abcd58 | 25945    | Statische Luminance Regex und Caching der Ergebnisse |
| c06e889 | 24100    | PortRenderer Builder & JSInterop optimierungen |
| 014ee0d | 21610    | FunctionBlock Subkomponenten entfernen |
| 56738c2 | 18855    | Unnötige divs entfernen |
| fcf3699 | 12264    | OnResize bei erstem Durchlauf unterbinden & kleinere NodeRenderer Optimierungen |
