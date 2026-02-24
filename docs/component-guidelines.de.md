# Guidelines für Blazor Komponenten

Dieses Dokument soll Regeln und Empfehlungen für die Entwicklung von Blazor Komponenten für den **ViciOne Cluster Editor** festlegen und zum Nachlesen bereitstellen.

## Code in Razorpages

### Code Behind Pattern

Es wird das Code-Behind-Pattern verwendet. Das heißt der `C#` -Code zu einer Razor-Datei liegt in einer externen `.cs`-Datei.

`/{Component}.razor`
``` razor
<div>
    @Reverse("argument")
</div>
```

`/{Component}.razor.cs`
``` csharp
namespace Namespace
{
    public sealed partial class Component : ComponentBase
    {
        private string Reverse(string argument) => string.Concat(argument.Reverse());
    }
}
```

## Attribute

### Reihenfolge

Die Reihenfolge der Attribute an Komponenten ist wie folgt:
- Attribute beginnend einem `@`
- Attribute, welche Eigenschaften der Komponenten sind (beginnen mit einem großen Buchstaben)
- Alle anderen Attribute

Die Attribute innerhalb der Gruppen werden immer alphabetisch sortiert.

```razor
<DxTextBox @bind-Text="ConfiguringConnection.Name"
           @onfocusout="OnConnectionNameFocusOut"
           ClearButtonDisplayMode="@DataEditorClearButtonDisplayMode.Auto"
           ReadOnly="@(!_canEditConnectionName)"
           autocomplete="off"
           maxlength="@_nameMaxLength" />
```

### Klammern

Um Compiler-Fehler zu vermeiden sollten Klammern überall da gesetzt werden wo Lambdafunktionen übergeben werden. 

``` razor
<Component Attribute="@(() => Method(param))">
```

### Leerzeichen

Vor `/>` wird ein Leerzeichen gesetzt, vor `>` aber nicht.

### Leerzeilen

Wenn eine Komponente mehr als ein Attribut hat, so werden alle Attribute auf eine eigene Zeile geschrieben und auf die Spalte des ersten Attributes eingerückt.
Falls eine mehrzeilige Komponente Kinder hat, so ist vor dem ersten Kind eine Leerzeile einzufügen.

```
<button @onclick="OnButtonClicked"
        class="btn btn-success"
        type="button">

    <span class="mdi mdi-plus">
</button>
```

### DO's and DON'Ts

Keine doppelten `@` in einem Attribut verwenden. Wenn das Attribut mit einem `@` beginnt, kann der Rest ohne geschrieben werden.\
❌ `@bind-Text="@ConfiguringConnection.Name"`\
✔️ `@bind-Text="ConfiguringConnection.Name"`

Keine unnötigen Lambdafunktionen verwenden.\
❌ `@onfocusout="(() => OnConnectionNameFocusOut())"`\
✔️ `@onfocusout="OnConnectionNameFocusOut"`

Lambdafunktionen sollten verwendet werden, um...

- Parameter zu übergeben\
✔️ `@onfocusout="(() => OnConnectionPropertyChanged(counter))`
- Paramter entgegenzunehmen\
✔️ `@onfocusout="((args) => OnConnectionPropertyChanged(args))`
- asynchrone Funktionen zu rufen\
✔️ `@onfocusout="(async () => await OnConnectionPropertyChangedAsync())`

Kein Code in Lambdafunktionen 'verstecken'. Dieser Code sollte im allgemeinen immer in der Code-behind Klasse sitzen.\
❌ `@onfocusout="(() => { ValidateForm(); _connectionChanged = true; })"`\
✔️ `@onfocusout="OnConnectionNameFocusOut"`
