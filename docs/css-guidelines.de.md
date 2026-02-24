# CSS Guidelines

Dieses Dokument soll Regeln und Empfehlungen für die Entwicklung von CSS für **ViciOne Cluster Editor** UI-Komponenten festlegen und zum Nachlesen bereitstellen.

## Z-Index

Neue Elemente haben einen Startindex von 100.000.

```
E1  >> z-index: 100.000
```

Elemente, die sich `darüber` oder `darunter` einordnen, verändern ihren Index in 10.000er Schritten.

````
E2  >> z-index: 110.000 [über E1]
E3  >> z-index: 120.000 [über E2]
````

Elemente, die sich später dazwischen eingliedern, wählen den `mitteleren Index` zwischen den beiden umschließenden Elementen.

````
E4  >> z-index: 115.000 [zwischen E1 & E2]
````

Elemente, die dann wieder darüber/darunter liegen, verändern den Index um einen entsprechend kleineren Wert.

```
E5  >> z-index: 114.000 [unter E4]
E6  >> z-index: 113.000 [unter E5]
```

Weitere Beispiele:
```
E7  >> z-index: 113.500 [zwischen E5 & E6]
E8  >> z-index: 113.000 [gleich E6]
E9  >> z-index: 113.250 [zwischen E7 & E8]
E10 >> z-index: 113.260 [über E9]
```
