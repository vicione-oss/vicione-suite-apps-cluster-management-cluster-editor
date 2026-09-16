# Blazor component guideline


## Code behind pattern

Try to always use the "Code Behind" pattern when possible.

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

## Attributes

### Ordering

The attribues of a component are ordered alphabetically within these groups
- Attributes starting with `@`
- Properties of the component
- Everything else

```razor
<DxTextBox @bind-Text="ConfiguringConnection.Name"
           @onfocusout="OnConnectionNameFocusOut"
           ClearButtonDisplayMode="@DataEditorClearButtonDisplayMode.Auto"
           ReadOnly="@(!_canEditConnectionName)"
           autocomplete="off"
           maxlength="@_nameMaxLength" />
```

### Brackets  

Lambda functions should always be enclosed in brackts.

``` razor
<Component Attribute="@(() => Method(param))">
```

### Whitespace

Add one space before `/>`, but not before `>`.

### Child content

If a component has more than one attribute, write each attribute on a new line, indented to the column of the first attribute.
If the component has child content, add an empty line.

```
<button @onclick="OnButtonClicked"
        class="btn btn-success"
        type="button">

    <span class="mdi mdi-plus">
</button>
```

### DO's and DON'Ts

Don't use multiple `@` in one attribute.\
❌ `@bind-Text="@ConfiguringConnection.Name"`\
✔️ `@bind-Text="ConfiguringConnection.Name"`

Don't use unnecessary lambda functions.\
❌ `@onfocusout="(() => OnConnectionNameFocusOut())"`\
✔️ `@onfocusout="OnConnectionNameFocusOut"`

Use lambda functions to

- Pass paramenter\
✔️ `@onfocusout="(() => OnConnectionPropertyChanged(counter))`\
✔️ `@onfocusout="((args) => OnConnectionPropertyChanged(args))`
- Call asynchronous methods\
✔️ `@onfocusout="(async () => await OnConnectionPropertyChangedAsync())`

Don't 'hide' code in lambda functions, use the code behind file for this.\
❌ `@onfocusout="(() => { ValidateForm(); _connectionChanged = true; })"`\
✔️ `@onfocusout="OnConnectionNameFocusOut"`
