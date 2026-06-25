# Changelog

## 1.3.0 - Unreleased

### Changed

- Exchanged legacy scss color variables with these from the `ViciOne.Ui.Design` library
- Renamed `DisplayText` property to `Name` for TreeNodes

## 1.2.0 - 2026-06-17

### Added

- Added shift-click range selection for connectors in `ContainerEditor`
- Added `PropertyGrid` support for Container in `ContainerEditor`

### Changed

- Restructured CHANGELOG, moved fixes to their own section
- Double click on a `Connector` container marker now selects the associated parent container's `Connector`
- Double click on a `Link` now selects the associated Connector instead of the Node 
- Reduced GC pressure on interaction hot paths by replacing LINQ chains with allocation-free loops, eliminated spread-operator array copies in event buffers, fixed quadratic diagram filter lookup

### Fixed

- Fixed moving nodes could not return to their start positions
- Fixed `Viewport` not restored correctly after container change
- Fixed wrong container position on FunctionBlock "Move to new container"
- Fixed `Playwright` end-to-end tests

### Updated external references

- `ViciOne.Ui.Blazor.Components` package, updated to version `5.14.0`
- `ViciOne.Ui.Design` package, added in version `2.1.0`
- `ViciOne.Ui.Localization` package, updated to version `3.4.0`
- `ViciOne.Ui.MonochromeIcons` packages, updated to version `4.11.0`

## 1.1.0 - 2026-06-04

### Changed

- Moved `ViciOne.Ui.Shared.Dx.Components.UniversalInput` to `ViciOne.Ui.ClusterEditor.Components.UniversalInput`
- Replaced `System.Timers.Timer` with `CancellationTokenSource` based approch

### Fixed

- Fixed collection modified exception from `DiagramScrollbarAdapter`
- Fixed capitalization changes in text properties were not recognized
- Fixed `ContainerBreadcrumb` padding and margin
- Fixed possible application crash when trying to modify values in `FbSettingsEditor`

## 1.0.0 - 2026-05-28

### Added

- Added license
- Added deactivated FunctionBlock representation
- Added dynamic expand/collapse button states in `Library` and `Cluster Topology` section
- Added `CancellationToken` support to `ClusterEditorManagement` (external interface)

### Changed

- Updated to .NET 10
- Replaced local Breadcrumb implementation with `ViciOne.Ui.Blazor.Components.Breadcrumb`
- Replaced `DxSpinEdit` with `ViciOne.Ui.Blazor.Components.SpinEdit`
- Replaced `DxToolbar` in MainToolbar with `ViciOne.Ui.Blazor.Components.Toolbar`
- Replaced `ViciOne.Ui.Shared.Dx.SearchBox` with `ViciOne.Ui.Blazor.Components.SearchBox`
- Improved expand & collapse button states in `DataPortSection`
- Exchanged various SVG icons with icons from `ViciOne.Ui.MonochromeIcons`
- Changed signature of `LoadDataflow` method from void to Task in `DataManagementService`
- Double click on nodes in `DataPorts` and `Published Conntectors` section highlights the corresponding markers
- Removed dependency to `ResizeObserverContainer`
- Improved node selection ordering method
- Improved node move handling when releasing the cursor outside of the `NodeEditor` area
- Optimized code for name field height measurement of nodes
- Optimized setting zoom/pan after cluster load
- Unified the column chooser icon
- Moved `ViciOne.Ui.Shared.Dx.Components.Scrolling` to `ViciOne.Ui.ClusterEditor.Components.Scrolling`
- Changed sort order in `DataPort` edit mode `PropertyGrid`
- Respect `TransferDirection` restrictions of `DataPortChildNodes` during drag & drop
- Improved `Library` drag async handling
- Reworked library drag to use JavaScript based movement
- Prevent horizontal scrollbar in `DataPortSection` 
- Implemented right-aligned sticky action buttons for `DataPortTreeNodes`

### Fixed

- Fixed potential wrong link positions of FunctionBlocks on first move after load
- Fixed DataPort highlighting
- Fixed possible 'Create a new DataPort' bug after loading a cluster
- Fixed context menu in `Settings Editor`
- Fixed diagram node movement on `Egde Dragging Area`
- Fixed `Search & Tools` clear filter button
- Fixed tooltips getting stuck
- Fixed potential wrong node size in diagram area or minimap after loading a cluster
- Fixed sorting of dataflows after rename
- Fixed cancellation of chnges on `X` button of `ContainerEditor` dialog
- Fixed pan behavior initialization in suite context
- Fixed wrong `Connector` positions after library drag

### Removed

- Removed editor version from `InfomationSection`

### Updated external references

- `Microsoft` packages, update to version `10.0.8`
- `Microsoft.Extensions.Diagnostics.Testing` package, updated to version `10.2.0`
- `Microsoft.NET.Test.Sdk` package, updated to version `18.0.1`
- `ViciOne.Cluster.Builder` package, updated to `1.0.0`
- `ViciOne.Ui.Shared.Dx` package, updated to `0.19.1`
- `ViciOne.Ui.Blazor.Components` package, updated to version `5.13.0`
- `ViciOne.Ui.Design` package, added in version `2.0.3`
- `ViciOne.Ui.Localization` package, updated to version `3.3.0`
- `ViciOne.Ui.MonochromeIcons` packages, updated to version `4.10.0`
- `ViciOne.Ui.Testing.Playwright` package, updated to version `1.0.0`
- `ViciOne.Ui.TreeEditor` package, updated to version `2.0.1`
- `xunit.runner.visualstudio` package, updated to version `3.1.5`
- `xunit.v3` package, updated to version `3.2.2`
- `Z.Blazor.Diagrams` package, updated to version `3.0.4`
- `ViciOne.TreeBuilder` package, updated to version `1.1.1`
- `ViciOne.Core.Dataflow.DataModel.Generation` package, updated to version `1.0.0`
- `ViciOne.Suite.System.DataPort` package, updated to version `1.0.0`

## 0.17.0 - 2025-12-08

### Added

- Implemented dependencies between dataport node properties
- Implemented prevent context menu on block marker and its line

### Changed

- Replaced `ViciOne.Ui.Shared.Dx.PropertyGrid` with `ViciOne.Ui.Blazor.Components.PropertyGrid`
- Replaced `DxCheckBox` with `ViciOne.Ui.Blazor.Components.CheckBox` in `FbSettingsEditor`
- Exchanged and removed various SVG icons with icons from `ViciOne.Ui.MonochromeIcons`
- Exchanged and removed all MDI icons with icons from `ViciOne.Ui.MonochromeIcons`
- Exchanged temp icon for minimap with correct icon from `ViciOne.Ui.MonochromeIcons`
- Exchanged all PNG icons used in context menus with icons from `ViciOne.Ui.MonochromeIcons`
- Rearranged filter buttons and added filter to select all input and output connectors without system connectors to `ConnectorsSelectionDialog`
- Adjusted the `Topology` to ensure that only one node can be edited at a time
- Reworked events of `IDataManagement` to support async handlers and proper exception handling

### Fixed

- Fixed `Container Editor` zoom button tooltip
- Fixed `Container Editor` list element height
- Fixed possible `KeyNotFoundException` in `DataflowStructureTreeAdapter`
- Fixed context menu entries for `Connectors` after jumping to the connector
- Fixed selection synchronization bug in `ContainerEditor`
- Fixed context menu is complete empty on block node marker right-click
- Fixed misaligned search tools in sections

### Removed

- Removed `ContextMenu` implementation guide, moved to [`ViciOne.Ui.Blazor.Components.ContextMenu`](https://gitlab.i40.ifm-datalink.net/acx/vo-ui/vo-blazor-components/-/blob/master/src/ViciOne.Ui.Blazor.Components/ContextMenu/README.md)
- Removed update of property grid in `PropertySection` if `ContainerEditor` is opened

### Updated external references

- `AspNetCore.SassCompiler` package, updated to version `1.93.2`
- `Microsoft.AspNetCore.Components.WebAssembly` package, updated to version `9.0.10`
- `Microsoft.AspNetCore.Components.WebAssembly.DevServer` package, updated to version `9.0.10`
- `Microsoft.AspNetCore.Components.WebAssembly.Server` package, updated to version `9.0.10`
- `Microsoft.AspNetCore.Mvc.Testing` package, updated to version `9.0.10`
- `Microsoft.Extensions.Configuration` package, updated to version `9.0.10`
- `Microsoft.Extensions.Configuration.Binder` package, updated to version `9.0.10`
- `Microsoft.Extensions.Localization` package, updated to version `9.0.10`
- `System.Reflection.MetadataLoadContext` package, updated to version `9.0.10`
- `ViciOne.Core.Dataflow.DataModel.Generation` package, updated to version `0.51.0`
- `ViciOne.ManagedEngine.Serialization` package, updated to version `0.62.0`
- `ViciOne.Suite.System.DataPort` package, updated to version `0.15.0`
- `ViciOne.TreeBuilder` package, updated to version `0.6.0`
- `ViciOne.Ui.Blazor.Components` package, updated to version `4.3.0`
- `ViciOne.Ui.Localization` package, updated to version `2.33.0`
- `ViciOne.Ui.MonochromeIcons` packages, updated to version `3.9.0`
- `ViciOne.Ui.Shared.Dx` package, updated to version `0.18.0`
- `ViciOne.Cluster.Model` package, updated to `0.11.0`
- `ViciOne.Cluster.Builder` package, updated to `0.11.0`
- `ViciOne.Ui.TreeEditor` package, updated to version `0.12.0`

## 0.16.1 - 2025-10-22

### Added

- Reenabled label editor
- Added highlighting of jump labels as predecessor or successor of selected blocks
- Added default sizes for dialog popups

### Changed

- Reworked dependency loading in frame app
- Changed Published Connector tooltips to show the full path
- Renamed `FunctionBlockLinkComponent` to `BlockLinkComponent`
- Replaced some context menu icons with SVGs or, if available, icons from the `MonochromeIcons` library
- Changed `FrontColor` and `BackColor` properties of `FunctionBlock` to be nullable
- Improved `ContainerBreadcrumb` disposal
- The add `DataPorts` context menu entries in the `DataPortSection` are now disabled if the maximum count of a specific `DataPort` is reached
- Changed the `Type` property of a `ClusterApplication` to be read only
- Changed `Container Editor` popup height to a relative value

### Fixed

- Fixed `ColorableIcons` tests
- Fixed initial link positions on FBs with long names
- Fixed value setting of `ValueType` property
- Fixed a bug that appeared after deleting a currently edited `DataPortNode` and trying to edit a newly added `DataPortNode`
- Fixed a minor logic issue when setting the ValueType property
- Fixed some issues with the `PublishedConnectorsSection` context menu
- Fixed `PublishedConnectorMarker` selection line still visible after deletion
- Fixed `PropertyGrid` is not cleared during `Container` change
- Fixed wrong handling of null or empty values in `AlphaNumericComparer`
- Fixed `Cluster Topology` section scrollbar

### Removed

- Removed unused `Colors.cs` constants file
- Removed unused elements from `MainToolbar`, `FileSection`, `DataPortsSection` and `SearchAndToolsSection`
- Removed `Import` and `Export` buttons from `TopologySection`
- Removed unused FileSection buttons in release mode; they still exist in debug mode
- Removed unused elements from `NodeEditorContextMenu`

### Updated external references

- `Markdig` package, added in version `0.42.0`
- `ViciOne.Cluster.Builder` package, updated to version `0.11.0`
- `ViciOne.CodeAnalysis` package, updated to version `1.4.0`
- `ViciOne.Ui.Design` package, added in version `1.0.1`
- `ViciOne.Ui.Shared.Dx` package, updated to version `0.16.2`

## 0.16.0 - 2025-07-10

### Changed

- Increased ClusterBuilderEventBuffer timer interval
- ValueType property in `DataPortTreeNode` now only appears if the node has at least one valid data type
- `FbSettingsEditor`
    - Unused template buttons have been hidden
    - Comment column in `ColumnChooser` have been hidden and fixed on the right side
    - Implemented keyboard support for checkboxes
    - Fixed keyboard navigation and operation
- Reworked node movement to be calculated in JavaScript

### Fixed

- Fixed property grid during "move to" and "dissolve" container operations
- Fixed resizing label render delay
- Fixed movement of other selected nodes while resizing a label

### Updated external references

- `AspNetCore.SassCompiler` package, updated to version `1.89.2`
- `bunit` package, updated to version `1.4.0`
- `Microsoft.AspNetCore.Components.WebAssembly` package, updated to version `9.0.6`
- `Microsoft.AspNetCore.Components.WebAssembly.DevServer` package, updated to version `9.0.6`
- `Microsoft.AspNetCore.Components.WebAssembly.Server` package, updated to version `9.0.6`
- `Microsoft.AspNetCore.Mvc.Testing` package, updated to version `9.0.6`
- `Microsoft.Extensions.Configuration` package, updated to version `9.0.6`
- `Microsoft.Extensions.Configuration.Binder` package, updated to version `9.0.6`
- `Microsoft.Extensions.Localization` package, updated to version `9.0.6`
- `Microsoft.NET.Test.Sdk` package, updated to version `17.14.1`
- `Microsoft.Playwright` package, updated to version `1.53.0`
- `ViciOne.Cluster.Builder` package, updated to version `0.10.0`
- `ViciOne.Cluster.Model` package, updated to version `0.10.0`
- `ViciOne.Core.Dataflow.DataModel.Generation` package, updated to version `0.48.0`
- `ViciOne.ManagedEngine.Serialization` package, updated to version `0.61.0`
- `ViciOne.Ui.Blazor.Components` package, updated to version `3.8.5`
- `ViciOne.Ui.MonochromeIcons` packages, updated to version `3.6.0`
- `ViciOne.Ui.Shared.Dx` package, updated to version `0.14.0`
- `xunit.runner.visualstudio` package, updated to version `3.1.1`

## 0.15.0 - 2025-05-22

### Added

- Added Drag&Drop of DataPoints to Connectors

### Changed

- Reworked `DataPortTreeAdapter` initialization

### Updated external references

- `AspNetCore.SassCompiler` package, updated to version `1.88.0`
- `Microsoft` packages, updated to version `9.0.5`
- `Microsoft.Playwright` package, updated to version `1.52.0`
- `ViciOne.Ui.TreeEditor` package, updated to version `0.9.0`
- `ViciOne.Ui.Shared.Dx` package, updated to version `0.12.0`
- `xunit.runner.visualstudio` package, updated to version `3.1.0`

## 0.14.0 - 2025-04-30

### Added

- Added tooltip to `DataPortTreeNodes` to show linked `Connectors`

### Changed

- Reworked loading FBs
- Reworked 'DataPorts' section ux
    - No entries will be removed from the '+' menu anymore
    - A click on an entry in the '+' dataports menu always creates a data port and not only the category.
    - After the data port creation with the '+' dataports button the newly created data port will be selected und jumped to it. If the category was collapsed it will be expanded.
    - Deleting the last data port in a category will also delete the category.
- Support pooling symbol for unknown pooling modes
- Optimised `PropertyChanged` calls druing engine change in `Datastore`
- Implemented jump to linked `Connectors` on `DataPortTreeNodes`
- Extend FunctionBlock cycle frequency to 2 Digits
- Changed tooltip position calculation to use parent bounds

### Fixed

- Fixed `DataPort` render issue after deleting DataPorts
- Fixed main toolbar not displaying all icons
- Fixed tooltips remaining on diagram when block with active tooltip is deleted

### Updated external references

- `AspNetCore.SassCompiler` package, updated to version `1.87.0`
- `Microsoft` packages, updated to version `9.0.4`
- `ViciOne.Cluster.Builder` package, updated to version `0.9.0`
- `ViciOne.Cluster.Model` package, updated to version `0.9.0`
- `ViciOne.Ui.Blazor.Components` package, updated to version `3.6.0`
- `ViciOne.Ui.Localisation` package, updated to version `2.25.0`
- `ViciOne.Ui.MonochromeIcons` packages, updated to version `3.4.0`
- `ViciOne.Ui.Shared.Dx` package, updated to version `0.11.1`

## 0.13.0 - 2025-03-07

### Changed

- Refactored `ClusterBuilder` event handling
- Improved selection of block nodes

## 0.12.0 - 2025-03-05

### Updated external references

- `AspNetCore.SassCompiler` package, updated to version `1.85.1`
- `ViciOne.Cluster.Builder` package, updated to version `0.8.0`
- `ViciOne.Cluster.Model` package, updated to version `0.8.0`
- `ViciOne.Core.Dataflow.DataModel.Generation` package, updated to version `0.47.0`
- `ViciOne.Ui.Blazor.Components` package, updated to version `3.3.2`

## 0.11.0 - 2025-02-27

### Fixed

- Fixed end to end tests
- Fixed new Visual Studio 17.13.0 analyzer messages
- Fixed FB context menu not showing up in specific situation

### Removed

- Removed unnecessary css and js imports

### Updated external references

- `AspNetCore.SassCompiler` package, updated to version `1.85.0`
- `ViciOne.Ui.Blazor.Components` package, updated to version `3.2.0`
- `ViciOne.Ui.Localisation` package, updated to version `2.21.0`
- `ViciOne.Ui.MonochromeIcons` packages, updated to version `3.3.0`
- `ViciOne.Ui.Shared.Dx` package, updated to version `0.9.0`

## 0.10.0 - 2025-02-13

### Changed

- Improved Published Connector grid performance
- Activated `ShowAllRows` in `FbSettingsEditor` to prevent unnecessary paging of settings

### Updated external references

- `ViciOne.Cluster.Builder` package, updated to version `0.7.0`
- `.NET` packages, updated to version `9.0.2`
- `Microsoft.Playwright` package, updated to version `1.50.0`

## 0.9.0 - 2025-02-07

### Added

- Build target to automatically install required node modules and run the npm build script if needed after checkout

### Changed

- Updated `.editorconfig` to newest version
- Custom `@mdi` implementation to use `@use` instead of `@import` and disabled SASS deprecation warnings
- Improved callings to `StatisticService` during bulk operations
- Improved `ContainerBreadcrumb` disposal
- Improved `FbSettingsEditor` and `TopologyTreeAdapter` event handling
- Improved DataPort section memory handling
- Reworked tooltips

### Removed

- Removed default net9.0 asset compression

### Updated external references

- `AspNetCore.SassCompiler` package, updated to version `1.83.4`
- `bunit` package, updated to version `1.38.5`
- `.NET` packages, updated to version `9.0.1`
- `ViciOne.Cluster.Builder` package, updated to version `0.6.0`
- `ViciOne.Cluster.Model` package, updated to version `0.6.0`
- `ViciOne.Ui.Blazor.Components` package, updated to version `3.1.0`
- `ViciOne.Ui.MonochromeIcons` packages, updated to version `3.2.1`
- `ViciOne.Ui.Shared.Dx` package, updated to version `0.8.0`
- `ViciOne.Ui.TreeEditor` package, updated to version `0.8.0`

## 0.8.0 - 2024-12-18

### Added

- Support readonly node names in DataPort tree
- Support transfer directions on every level in DataPort tree

### Changed

- Group node properties by category in DataPort tree
- Use the highest transfer direction as default in DataPort tree

### Updated external references

- `AspNetCore.SassCompiler` package, updated to version `1.83.0`
- `bunit` package, updated to version `1.37.7`
- Framework updated to `net9.0`
- `Microsoft.Playwright` package, updated to version `1.49.0`
- `ViciOne.Cluster.Builder` package, updated to version `0.5.0`
- `ViciOne.Ui.Blazor.Components` package, updated to version `3.0.0`
- `ViciOne.Ui.MonochromeIcons` package, updated to version `3.0.0`
- `ViciOne.Ui.Shared.Dx` package, updated to version `0.7.0` (DevExpress `24.2.3`)
- `ViciOne.TreeBuilder` package, updated to version `0.5.0`
- `ViciOne.Ui.TreeEditor` package, updated to version `0.7.9`
- `Z.Blazor.Diagrams` package, updated to version `3.0.3`

## 0.7.0 - 2024-12-02

### Changed

- Refactored `Minimap` rendering
- Avoid `ContextMenu` requests during various operations
- Moved `PopupRoot` to `MainLayout`

### Fixed

- Fixed pan/zoom bug in release configuration

## 0.6.0 - 2024-11-18

### Added

- Added unsubscribe of `BuilderChanged` event to `PropertyMapperBase`

### Changed

- Changed search box padding in Dataflow section
- Increased debug section maximum cluster file upload size
- Optimized event buffering, fixed some crashes with very large clusters

### Fixed

- Fixed rename dataflow bug
- Fixed an exception during task disposing in `ContainerBreadcrumb`
- Fixed logic asking whether Builder is available in `ClusterBuilderBuffer`
- Fixed bug when setting a pan behavior

### Updated external references

- `AspNetCore.SassCompiler` package, updated to version `1.80.6`
- `ViciOne.Ui.Blazor.Components` package, updated to version `1.10.1`
- `ViciOne.Ui.MonochromeIcons` package, updated to version `2.2.0`
- `ViciOne.Ui.Shared.Dx` package, updated to version `0.6.0` (DevExpress `24.1.7`)

## 0.5.0 - 2024-10-29

### Changed

- Redesign of Search & Tools section header
- Filter results in trees now always show child nodes
- Main toolbar aligned in the center

### Fixed

- Fixed some grid related issues during value editing in `FbSettingsEditor`
- Fixed port positions after FunctionBlock dragging operation
- Fixed deleting containers

### Updated external references

- `ViciOne.TreeBuilder` package, updated to version `0.3.0`

## 0.4.0 - 2024-10-23

### Added

- DataPort tree is now searchable
- Buffering service for ClusterBuilder events
- Interface incl. service to change diagram settings
- Ability to create, remove and rename dataflows from `DataflowStructureTree`

### Changed

- Refactored scss files to use `use` instead of `import` and partials for colors.scss and mixins.scss
- Toolbars on the left and right side now remember last configured width when switching from compact to fluid mode
- Redesign of Library section header
- Disabled unnecessary drag and drop options of the Dataflow, DataPort and Cluster Topology tree
- Redesign of Cluster Topology section header
- Redesign of Property section header
- Increase vertical size of dataport dropzones
- Moved diagram settings buttons from demo section to debug section
- Use DataPort root name as display text instead of identifier in add DataPort context menu

### Removed

- Removed DataPort default engine assignment
- Removed demo section
- Removed external toolbar content render fragment

### Updated external references

- Versions of included `FunctionBlocks` and `DataPorts`
- `ViciOne.Ui.Blazor.Components` package, updated to version `1.7.4`
- `ViciOne.Ui.MonochromeIcons` package, updated to version `2.1.0`
- `ViciOne.Ui.Shared.Dx` package, updated to version `0.3.0.1371561`
- `ViciOne.Ui.TreeEditor` package, updated to version `0.7.4`
- `ViciOne.Cluster.Builder` package, updated to version `0.4.0`
- `ViciOne.TreeBuilder` package, updated to version `0.2.0`

## 0.3.0 - 2024-09-30

### Added

- Basic render end-to-end tests for all sections, test for File -> New
- Loading a container without values for Viewport or Zoom results in an automatic ZoomToFit

### Changed

- Use of `SectionRail` component
- DataPort FB is now hidden in FB library
- ZoomToFit centers the shown objects
- Zoomfactor in ZoomToFit is now restricted to defined minimum and maximum, minimum is changed from 0.10 to 0.01
- Allow expanding multiple root nodes in the DataPort tree
- Prevent unwanted changes of data port tree nodes in edit mode
- Changed visibility and enabled logic of action buttons in dataport tree
- Cleaned up extensions in data port section
- Replaced custom library tree with `TreeEditor` implementation
- Unified FB library filter header with other section headers

### Fixed

- Fixed column chooser position in some dialogs and the published connectors section
- Fixed name creation of dataport elements if name is doubled
- Fixed support of possible values in data port tranfer mode properties
- Fixed Dataflow structure tree being empty on filter directly after initial load

### Updated external references

- `ViciOne.Cluster.Model` package, updated to version `0.2.0.1310163-ci`
- `ViciOne.Cluster.Builder` package, updated to version `0.2.0.1310163-ci`
- `ViciOne.Ui.Blazor.Components` package, updated to version `1.5.0`
- `ViciOne.Ui.MonochromeIcons` package, updated to version `1.15.0`
- `ViciOne.Ui.Shared.Dx` package, updated to version `0.2.0.1328532`
- `ViciOne.Ui.TreeEditor` package, updated to version `0.7.1.1338654`

## 0.2.2 - 2024-09-09

### Fixed

- Fixed block node context menu if no engines are available

## 0.2.1 - 2024-09-06

### Added

- Dataflow selection to dataflow tree

### Changed

- Prevent linking connectors to datapoints in edit mode

### Fixed

- Fixed `Container Editor` selection
- Fixed exception in `DataflowStructureTreeAdapter` when adding a FunctionBlock

## 0.2.0 - 2024-09-03

### Changed

- `ViciOne.Cluster.Builder` package, updated to version `0.2.0.1307982-ci`
- `ViciOne.Ui.Shared.Dx` package, updated to version `0.2.0.1310653`
- `ViciOne.Ui.TreeEditor` package, updated to version `0.7.0`
