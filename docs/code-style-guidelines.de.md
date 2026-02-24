# Style Guidelines für Quellcode

Dieses Dokument stellt Guidelines für das Schreiben von Quellcode Dateien bereit.

## C#

### Sortierung in Typen

Die Bestandteile einer C# Klasse sowie eines Structs, Interfaces und Records werden nach Gruppen sortiert.

Gruppen sind durch eine Leerzeile voneinander getrennt.

1. Konstanten
   - Gruppiert nach Zugriffsmodifizierer in der Reihenfolge `private`, `protected`, `internal`, `public` gefolgt von `static`
   - Jede Gruppe anhand des Feldnames alphabetisch sortiert
2. Statische Felder 
   - Alphabetisch sortiert basierend auf Feldnamen
3. Andere Felder
   - Alphabetisch sortiert basierend auf Feldnamen
4. Eigenschaften
   - Gruppiert nach ihrem ersten Attribut
   - Gruppen anhand des ersten Attributnames alphabetisch sortiert
   - Eigenschaften ohne Attribut werden in eine Gruppe ohne Attribute eingeordnet, diese Gruppe kommt zuletzt
5. Ereignisse
   - Alphabetisch sortiert basierend auf Eventnamen
6. Konstruktoren
   - Gruppiert nach Schlüsselwort in der Reihenfolge `static`, `public`, `internal`, `protected`, `private` gefolgt von Konstruktoren ohne Schlüsselwort
   - Gruppen nach Anzahl der Parameter aufsteigend sortiert
7. Methoden
   - Alphabetisch sortiert basierend auf Methodenname

### Sorting in Anweisungen

1. Konstruktoren
   - Eigenschaftenzuweisungen sind alphabetisch nach Eigenschaftsname sortiert
2. Objektinitialisierer
   - Zuweisungsausdrücke werden alphabetisch nach Eigenschaftsnamen sortiert

### Kommentare

Kommentare sollen auf Englisch geschrieben und groß und mit Leerzeichen begonnen werden.
`// This is a comment.`

## git

### Commit Messages

Commit Messages sollen in Englisch geschrieben und groß begonnen werden.
Commit Messages sollten im Imperativ geschrieben werden und den folgenden Satz ergänzen: "If applied, this commit will <commit message>",
z.B. "Release version 1.0.0" - "If applied, this commit will <release version 1.0.0>".
