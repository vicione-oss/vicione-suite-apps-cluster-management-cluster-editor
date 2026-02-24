# Debug-Console

The debug console is a combination of a component inside the debug sidebar menu and a service. Both of them and all the needed models are only included in a built version of the project when built in `DEBUG` mode / configuration.

## Attention to debugging code

> All the models, components and services are and should be only available when the project is built in `DEBUG` configuration. This means that when deployed or run in one of the existing pipelines without adjusting the parameters the build will fail.
> 
> THIS IS INTENSIONAL
>
> There should never be logging code within the finalised and ready to merge version of the code except it is only active when built with the debug configuration (`#if DEBUG [...] #endif`).

## Features

The following listing displays the full arrangement of features supported by the debug console and how to use them.

### Log and value view

Because of the restricted size given by the dataflow sidebar and within the debug section the console can't have unlimited space. Therefore two separate views display the values and the logged messages. The views can be toggled between by pressing the `Toggle log` button at the lower end of the console.

### Resize console "window"

As of the same reasons the message and value log use different views the console is restricted in height by default. To squeeze additional height out of the unused space at the lower end of the console a number input field is provided to add (or remove) pixels to the height of the console "window" / area.

### Change history sizes

> `DebugService.SetValueHistorySize(int)`

By accessing the `DebugService` the size of the value log history can be adjusted by using `DebugService.SetValueHistorySize(int)`. The default length is `10`.

### Write values for monitoring

> `DebugService.AddValue(string, string)`

The default view of the console is the value log. It provides the capability to watch changes to values as they happen. It also provides a history to all values logged. To log a value to the console call `DebugService.AddValue(string, string)`.

The first parameter is the name of the value displayed in the console. It's also used to uniquely identify the value resulting in overwriting the previous value when another value with the same name is added. This is the intended behaviour for the value view because it enables monitoring them without scrolling through hundreds of entries in a console. Every written value gets saved into the history for that exact name.

The second parameter represents the value itself.

### Write log messages

> `DebugService.Log(string)`

As there are two consoles in Blazor Server it's sometimes hard to spot to which the output of `Console.Out.WriteLine()` was written to. To simplify this and other useful functionality there is the log message view of the debug console. You can write messages to it by calling `DebugService.Log(string)`.

Messages written by that method automatically contain the caller member information (`CallerMemberName`, `CallerFilePath` and `CallerLineNumber`) to identify where the message was logged. This information can be displayed from within the UI when hovering over the circle (or pill if the message spans multiple lines) at the beginning of the log message.

### Add buttons to trigger custom actions

> `DebugService.SetAction(string, Action)`

If there is the need to trigger custom functions (for example to write some state to the message log) or invoke functions separately from their default call context a button with that function can be added to the console. To add a button call `DebugService.SetAction(string, Action)` with a `string` name displayed on the button and uniquely identifying the action and the action itself.

Please note that calling `SetAction` with the same `name` two times results in overwriting the action of the first call.

## Usage

To use the debug console just inject the `DebugService` into a service, component or page and use the instance from there with the methods available described above.

Injection within a component:

```csharp
[Inject]
private DebugService DbgService { get; set; } = null!;
```
