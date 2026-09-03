# Features (a comprehensive list)

This document contains available features in a very short form as an overview to what can be used and what could break with new features, bug fixes or updates. Once finished it should be updated every time a new feature is implemented, changed or removed to keep it as relatable as possible to the current version of the software.

> Table of contents
> - [Diagram](#diagram)
>     - [Pan behaviour - VO (default for RELEASE)](#pan-behaviour---vo-default-for-release)
>     - [Pan behaviour - GIMP (default for DEBUG)](#pan-behaviour---gimp-default-for-debug)
>     - [Shortcuts](#shortcuts)
>     - [Grid display mode](#grid-display-mode)
>     - [Selection](#selection)
>     - [Selection rectangle](#selection-rectangle)
>     - [Minimap](#minimap)
>     - [Dragging](#dragging)
>     - [Edge dragging](#edge-dragging)
>     - [Alignment](#alignment)
>     - [Context menu (CM)](#context-menu-cm)
>     - [Tooltips](#tooltips)
> - [Diagram canvas elements](#diagram-canvas-elements)
>     - [FunctionBlock (FB)](#functionblock-fb)
>         - [FB context menu](#fb-context-menu)
>         - [FB settings editor](#fb-settings-editor)
>     - [Container](#container)
>         - [Container context menu](#container-context-menu)
>     - [Connectors (FunctionBlock & Container)](#connectors-functionblock--container)
>         - [Container connectors](#container-connectors)
>         - [Flags](#flags)
>         - [Added to parent container](#added-to-parent-container)
>         - [Dataports](#dataports)
>         - [Published](#published)
>             - [Management dialog](#management-dialog)
>             - [Delete dialog](#delete-dialog)
>         - [Connector context menu](#connector-context-menu)
>     - [Link](#link)
>     - [Label](#label)
>         - [Label context menu](#label-context-menu)
> - [Toolbar - infrastructure (left)](#toolbar---infrastructure-left)
>     - [Expander](#expander)
>     - [Info section](#info-section)
>     - [Menu overlay](#menu-overlay)
> - [Toolbar - main (top)](#toolbar---main-top)
>     - [Zoom control](#zoom-control)
> - [Toolbar - dataflow (right)](#toolbar---dataflow-right)
>     - [Design / Library Tree](#design--library-tree)
>     - [Published connectors](#published-connectors)
>         - [Search and options](#search-and-options)
>         - [Published connector grid](#published-connector-grid)
>     - [Property grid](#property-grid)
>         - [Property list](#property-list)
>     - [Demo section](#demo-section)
>         - [Select blocks](#select-blocks)
>         - [Find Blocks](#find-blocks)
>         - [Filter Blocks](#filter-blocks)
>         - [Trace](#trace)
> - [Debug features](#debug-features)
>     - [Debug section (right toolbar)](#debug-section-right-toolbar)
>         - [Feature switches](#feature-switches)
>         - [Generate FunctionBlocks](#generate-functionblocks)
>         - [Debug console](#debug-console)
>     - [External debug toolbar](#external-debug-toolbar)
>         - [Language select](#language-select)
>         - [Save / Load / Clear Dataflows (local storage)](#save--load--clear-dataflows-local-storage)
>         - [Import / Export Dataflows (JSON file)](#import--export-dataflows-json-file)

---

## Diagram

Features of the canvas itself.

### Pan behaviour - VO (default for RELEASE)

- press & hold middle mouse button and move the mouse to pan
- mouse wheel +/- to zoom

### Pan behaviour - GIMP (default for DEBUG)

- press & hold middle mouse button and move the mouse to pan
- mouse wheel +/- to scroll vertically
- [SHIFT] + mouse wheel +/- to scroll horizontally
- [ALT] + mouse wheel +/- to zoom

### Shortcuts

- [SPACE] zoom to fit
- [ESC] (canvas focused) leave current container (one layer up)
- [ESC] (in DfE full screen (button within main toolbar)) leave DfE full screen mode
- [DEL] delete selected elements

### Grid display mode

- toggle between background styles for the canvas grid (currently available as button within debug section of right toolbar)
- available grid styles: none, lines, dotted

### Selection

- press [LMB] while hovering over an element to select it
- press [RMB] while hovering over an element to select it (if not already) and invoke the context menu
- selecting an element that is currently not selected by pressing [LMB] or [RMB] deselects any other currently selected element and selects the pressed on one
- holding down [CTRL] when pressing [LMB] to select elements enables multi selection
- some selections disable the ability to select other elements during multi select
    - if a connector is selected no other element can be included
    - if any other element then a connector is selected no connector can be included
- the property grid inside the right toolbar changed according to what elements are selected within the diagram

### Selection rectangle

- press and hold [LMB] directly over the canvas (without canvas objects in between) to start selecting
- move the mouse while holding [LMB] pressed to draw a selection rectangle
- let go of [LMB] to select the canvas objects below the drawn rectangle
- there is a priority order in which elements can / will be selected, if one element of a higher order (highest = 1) is included in the selection all lower order (lowest = 2) element  will not be included
    1. blocks, links and labels
    2. published connectors

### Minimap

- automatically resizes with diagram canvas (keeps the same aspect ratio and scales content to size)
- current viewport diagram displayed (rectangle with only borders)
    - drag the viewport inside of the minimap to pan the diagram
    - pan the diagram and the minimap viewport moves with it
- click on the minimap to pan the diagram to the related position
- disable / hide the minimap (button currently within main toolbar)
- gets visually and click transparent when something on the canvas gets dragged (incl. links, blocks, labels (also resize), selection rect) so that objects behind can be seen and interacted with
- can display the colors of diagram elements
    - blocks colored by name background color
    - labels colored by background and border color 
    - transparent labels will be displayed with dashed border

### Dragging

- press and hold [LMB] while over a diagram element to start dragging
- move the mouse to drag it around the canvas while holding [LMB]
- release [LMB] to release drop it at its current position
- nearing the edges of the canvas while dragging activates the edge dragging

### Edge dragging

- gets activated when something is dragged around (incl. links, blocks, labels (also resize), selection rect)
- invisible until pointer is close to the edges of the canvas
- when displayed there are three directions to scroll shown:
    - the one the pointer is over
    - the ones adjacent to the first on both sides
- when hovered over one of the direction panels the diagram scrolls periodically (one scroll step every 100ms) in the indicated direction
- dragged elements move with the pointer the whole time

### Alignment

- align the selected elements after a specific scheme
- available trough [main toolbar](#toolbar---main-top) and [context menu](#context-menu-cm)
- alignment schemes
    - top - elements will be aligned by `upper edge` of the `highest selected` one
    - top by lowest - elements will be aligned by the `upper edge` of the `lowest selected` one
    - bottom - elements will be aligned by the `bottom edge` of the `lowest selected` one
    - left - elements will be aligned by the `left edge` of the `most left selected` one
    - right - element will be aligned by the `right edge` of the `most right selected` one

### Context menu (CM)

- can be invoked over any element except links of the diagram
- invoke the CM by pressing [RMB] over an element or the diagram itself
    - invoking on the diagram itself de-selects any selected element
    - invoking on an element keeps the selection and displays the CM
- on basis of what elements are selected the CM displays all applicable CM entries
- some CM entries may be displayed because they are applicable but grayed out because they cannot be used for some reason right now
- available general (not element specific) context menu actions
    - quick actions
        - delete - removes the selected elements from the dataflow
    - alignment
        - align the selected elements after a specific scheme in relation to each other
        - disabled when only one element is selected

### Tooltips

- hovering ~1sec over certain elements brings up tooltips
- tooltips available on
    - [FunctionBlock](#functionblock-fb)
    - [Container](#container)
    - [Connector](#connectors-functionblock--container)
    - Engine display (FunctionBlock & Container)
- nested elements (like connectors on FBs and containers) enable fast switching (it does not require another ~1sec to display the new tooltip) between tooltips
- they stay at their initial display position until the tooltip changes (nested tooltips) or the mouse leaves the element

---

## Diagram canvas elements

The elements that can be dragged / added to the canvas visualising a dataflow.

### FunctionBlock (FB)

- has tooltips for the FB itself and the engine display field
- [DBLCLK] on the FB name brings up the property grid in the right toolbar and activates and highlights the input field to change the name without any more clicks

#### FB context menu

- engine assignment - assign the selected engine to the selected FBs and containers
- move to container - creates a new container and moves the selected elements into it
- connector selection - multi select specific connectors of the selected FBs and Containers
- settings
    - opens the FB settings dialog
    - disabled when no settings are available

#### FB settings editor

- provides additional settings to the ones accessible via property grid
- [DBLCLK] on a FB (or multiple if used with selection) opens settings editor dialog
- context menu also provides a way to access the dialog
- not every FB has such settings
    - dialog does not open by [DBLCLK]
    - context menu entry is disabled
    - still accessible when multi selected with FB that has settings
- to edit these settings
    - press `Edit` within the first column for the row you want to edit
    - edit the value (there will be a validation error message when the values does not match the defined criteria)
    - press `Save` (within first row) to save the new value (`Cancel` restores the last saved value before the edit started)
    - press `Ok` to close the dialog and save the values to the FB (`Cancel` restores the values from before the dialog was initially opened)
- the close button `x` in the top right corner of the dialog acts like the `Cancel` button in the lower right

### Container

- has tooltips for the container itself and the engine display field
- [DBLCLK] on the container name brings up the property grid in the right toolbar and activates and highlights the input field to change the name without any more clicks

#### Container context menu

- engine assignment
    - assign the selected engine to the selected FBs and containers
    - assigns the engine not directly to the container but rather to all function blocks contained within
- move to container - creates a new container and moves the selected elements into it
- dissolve container
    - removes the selected container and places its contents in the current container
    - disabled when multiple containers are selected
- connector selection - multi select specific connectors of the selected FBs and containers

### Connectors (FunctionBlock & Container)

- has tooltip
- press and hold [LMB] on an output connector to start drawing a link from it
- while drawing a link the connectors the link can be attached to will be highlighted

#### Container connectors

- are connectors added from inside of the container
- [DBLCLK] to jump into the container, focus the origin block and select the clicked connector

#### Flags

- flags are indicating some sort of change to the default connector
- inherit some level of interaction

##### Added to parent container

- shows if the connector is added to the parent container of its FB
- [DBLCLK] to jump out of the current container focus it and select the clicked connector

##### Dataports

- shows if and the amount of connected dataports for that connector
- currently unable to invoke/show because dataports are disable because of a missing implementation

##### Published

- shows if published connectors are connected to the connector and how many
- [LMB] select the clicked published connector flag (also works with selection rectangle and [CTRL+LMB] for multi select)
- [DBLCLK] invokes an action base on the mount of published connectors connected
    - 0 = opens the [published connectors](#published-connectors) section, expands only the groups containing the matching entry, scrolls it into view and selects it
        - groups that do not contain the entry keep their current expanded / collapsed state
        - if a filter or the search hides the entry it is only selected, so it shows up already selected once the filter is cleared (no scrolling in that case)
        - on a container the marker resolves to the published connector of the function block inside it, because publishing always applies to the underlying connector
    - 1 = jumps to the original published connector that is attached
    - \> 1 = opens the management dialog
- [DEL] detaches the published connector from the local connectors base on the amount attached
    - 0 = nothing
    - 1 = detaches the published connector from the local one
        - when executed with multi select of connectors that have multiple published connectors attached to them then every published connector will be detached without warning or notice
    - \> 1 = opens the delete dialog

###### Management dialog

- provides management capabilities when multiple published connectors are connected to a connector
- head on top of the table provides the same capabilities as the one in the published connector section of the right toolbar
- [DBLCLK] on a published connector row closes the dialog, jumps the canvas to the clicked connector and selects it
- [DEL] on a selected row open the deletion view
    - select the published connectors you want to detach by checking the checkbox of relevant rows
    - click `Delete` in the bottom right to detach the selected ones and close the dialog
- [ESC], `Cancel` (bottom right) or the close button (`x` top right) close the dialog without changes

###### Delete dialog

- provides a simple way to detach multiple published connectors from a connector
- head on top of the table provides the same capabilities as the one in the published connector section of the right toolbar
- select the published connectors to delete within the grid and click `Delete` in the bottom right to detach them and close the dialog
- [ESC], `Cancel` (bottom right) or the close button (`x` top right) close the dialog without changes

#### Connector context menu

- add connector to parent container
    - adds the connector to the parent container
    - only visible when the connector is not already added to the parent container
- remove from parent container
    - removes the connector from every container upwards of the current one
    - only visible when the connector is already added to the parent container
- remove from container
    - removes the connector from the current container and subsequently from every parent container
    - only visible when it is a container connector
- publish
    - publishes the connector
    - only visible when not already published
- cancel publication
    - cancels / ends the publication of the connector
    - only visible when not already published
- dataports
    - currently disabled

### Link

- drawn from an output connector to an input connector
- can be selected (currently only for deletion with [DEL])
- [DBLCLK] on a link selects (and pans the viewport to) the block at the opposite end of the link
- drawn links can only be attached to connectors with the same type (color)

### Label

- created by context menu on diagram
- always behind blocks and links
- can be resized
    - only resizable when unlocked
    - hover over any edge or corner until the cursor changes then press down [LMB]
    - while holding [LMB] resize the label to the size needed
    - release [LMB] to end the resizing
- can be locked (and unlocked again)
    - through diagram context menu
    - lock prevents selecting, moving and resizing
    - all labels within the dataflow get locked at the same time
    - new created labels while locked are unlocked until deselected for the first time

#### Label context menu

- move to container - creates a new container and moves the selected elements into it
- bring to front - makes the label the most front one
- send to back - makes the label the most back one

---

## Toolbar - infrastructure (left)

The left toolbar contains the main navigation elements.

- the toolbar can be collapsed by clicking the left pointing chevron
- to expand the toolbar any of the expanders or the info section can be clicked
    - clicking the hamburger menu button does not expand the toolbar
    - clicking an expander or the info section also activates the clicked element
- clicking the hamburger menu button at the top opens the menu overlay

### Expander

- toolbar is constructed from expander sections
- only a single expander can be open at once
- clicking an expander that is not active closes all open ones and expands the clicked one

### Info section

- expands below when clicked to show the contained information
- closes the info area when clicked again
- currently contains the version number or the git commit hash when deployed

### Menu overlay

- opened by the hamburger button in the top left of the left sidebar
- overlays the toolbar and part of the diagram if toolbar itself is closed
- can be closed
    - by pressing the [X] button in the top right corner of the header
    - by clicking anywhere over the grayed out dataflow editor
- contains menu items to manage the dataflow in general
    - new / new with wizard - currently unused
    - open - currently opens the "external" debug sidebar
    - save / save as - currently unused
    - exit - currently unused

---

## Toolbar - main (top)

The top toolbar contains general controls i.e. editing and alignment.

- full screen expand
    - expand just the dataflow editor to the whole screen
    - if expanded close with 
- minimap toggle > enable / disable the minimap
- toolbar overflow (dx feature), if there is not enough horizontal space overflowing toolbar items are hidden in a sub menu (`...` at the right end of toolbar)

#### Zoom control

- increase/decrease zoom by 5% using the +/- buttons
- select pre-configured zoom value from dropdown (including zoom to fit)
- enter zoom value by hand
- zoom restricted (min 10% - max 200%)

---

## Toolbar - dataflow (right)

The right toolbar contains everything relevant for editing a dataflow

### Library

- search input on top of the tree - filters the designs by name
- "open" (folder icon) button - triggers an event to load the available designs from backend
- "+" button - expands all tree branches
- "-" button - collapses all tree branches
- "?" button - currently unused

#### Design / Library Tree

- shows all available (loaded) FunctionBlock designs displayed as a tree
- tree structure matches the dotnet namespace of the designs
- every layer of the tree can be collapsed for a better overview
- tree leaves are the designs themselves that can be dragged onto the diagram canvas
    - dragging them onto the diagram adds them to the dataflow
    - during the initial dragging (library to canvas) the fully rendered FB is displayed
- [DBLCLK] on a design creates a new FB of that type in the center of the viewport

### Published connectors

- enables the management of all published connectors

#### Search and options

- search - filters the grid rows (excluding grouping rows) by the input provided
- select only inputs
    - displays only published inputs within the grid when enabled
    - can only be active if "select only outputs" is not active
- select only outputs
    - displays only published outputs within the grid when enabled
    - can only be active if "select only inputs" is not active
- expand all
    - expands all groups when grouped
    - only enabled when grouped by something
- collapse all
    - collapses all groups
    - only enabled when grouped by something
- column chooser
    - hide / show columns from grid
    - reorder columns

#### Published connector grid

- shows all available published connectors
- [DBLCLK] jumps the canvas to the matching published connector and selects it
- entries can be grouped by dragging [LMB] a column header in the group bar directly on top the table
- columns can be reordered by dragging them [LMB] to their new position
- columns can be sorted by [LMB] clicking the header
- when grouped the group rows can be expanded by [LMB] clicking them
- use [CTRL+LMB] and [SHIFT+LMB] to select multiple connector of the same type (matching input/output and connector type)
- [RMB] on an entry row brings up a context menu
    - remove - removes the published connector from every port and unpublishes it
- dragging an entry row onto the grid with [LMB] enables that the dragged published connector can be attached to a FB or container connector
- dragging and attaching also works with multiple selected connectors
- while dragging a published connector from the grid all possible connectors where it can be attached to will be highlighted
- the same published connector cannot be attached to the same connector twice

### Property grid

- search input on top of the property list - currently unused (placeholder)
- "group" button - groups the properties by their category (default)
- "list" button - displays the available properties as plain list

#### Property list

- displays the configurable properties of the within the diagram selected items
- properties are condensed to the ones that are available for every type of selected element, example:
    - FB, container and label share the properties `BackColor`, `X` and `Y`
    - FB and container also share `ForeColor`, `Description` and `Name`
    - selecting the all three types only displays the three properties they share
    - selecting only FB and container displays the three they share with label but also the three they only share with each other (in sum six properties are displayed)
- if a condensed property has the same value in all instances it will be filled into the editor
- if a condensed property has multiple different values on its instances
    - the property name gets highlighted in blue
    - the editor for the property is kept empty (as far as possible)
- if the in the editor entered values deviates from the default value of that property
    - the property name will be highlighted in bold white
    - the little square after the editor will get filled white
- the little square after the editor invokes a context menu on [LMB]
    - currently only visible when the property has a default value to reset to
    - reset - resets the value of the property to its default value (and according to each instance type when its a condensed property)

### Demo section

Features that are fully implemented but not yet have their final place inside the UI.

#### Select blocks

- selects blocks by a specific pattern
- patterns available
    - clear - clears the selection
    - all - selects all blocks currently on the diagram
    - invert - inverts the selection of blocks

#### Find Blocks

- find blocks by selecting matched ones
- matches blocks by name
- find variants
    - all - finds all blocks matching the input pattern
    - next - selects the next one within the list of matching block
    - previous - selects the previous one within the list of matching block

#### Filter Blocks

- filter blocks by hiding not matching ones from the diagram
- filter variants
    - clear - clears the filter and unhides all blocks
    - filter (by name) - filters the block based on the input name pattern
    - selected - shows only the selected blocks
    - attached - shows only the selected blocks and the ones directly attached to them

#### Trace

- highlights links leading to predecessors and successors for the selected blocks
- available trace variants
    - clear - clears the current traced route
    - predecessor - traces the blocks connected to the inputs of the selected block
    - predecessors - traces all blocks chained into the inputs of the selected block
    - successor - traces the blocks connected to the outputs of the selected block
    - successors - traces all blocks chained from the outputs of the selected block

---

## Debug features

Features implemented only for debugging.

### Debug section (right toolbar)

- a separate section within the right toolbar
- only visible and accessible when project running in `DEBUG` mode

#### Feature switches

- disable custom context menu - disables the custom context menu on [RMB]
- toggle block alignment border - enables / disables if the dashed alignment border around blocks an their flags is shown while dragging them around the canvas

#### Generate FunctionBlocks

- generate a specified amount of some FunctionBlock
- specify the amount and type (the name is the name of the design i.e. "ManualOpSwitch") using the two input fields and hit generate to automatically place them onto the canvas
- "Generate all library blocks" generates one block of each loaded FunctionBlock(-design)

#### Debug console

- console to write values and log messages to for monitoring
- all features listed and described [here](debug-console.en.md)

### External debug toolbar

- opened through the "Open" option within the menu overlay of the left toolbar

#### Language select

- open the "external" side panel by selecting "Open" in the infrastructure toolbar overlay menu
- select the language to use through the dropdown
- the page will reload and be displayed in the selected language

#### Save / Load / Clear Dataflows (local storage)

- open the "external" side panel by selecting "Open" in the infrastructure toolbar overlay menu
- three slots for saving and loading dataflows are available
- "Save" saves the current dataflow to the matching slot (attention: there is no overwrite protection if the slot is already in use)
- "Load" load the dataflow from the matching slot (attention: there is no overwrite protection for the currently active dataflow)
- "Clear" to clear the matching slot (attention: there is no protection for clearing)
- the dataflow to save can only be 5MB in size at max (restriction by the browser)
- larger dataflows have to use the import/export function

#### Import / Export Dataflows (JSON file)

- open the "external" side panel by selecting "Open" in the infrastructure toolbar overlay menu
- "Import" opens a file selection dialog where the dataflow JSON to import can be selected (attention: there is no overwrite protection for the currently active dataflow)
- "Export" generates a new JSON and downloads it

