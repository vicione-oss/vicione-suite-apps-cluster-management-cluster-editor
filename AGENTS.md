# ViciOne Cluster Editor — Project Overview for AI Agents

<!-- Verified against commit: d1cf2ffdf1e200fe81ccb2151fccfcf6faef41d1 on 2026/07/30 -->

> Orientation guide for AI coding agents working in this repository.
> **If a statement here contradicts the actual file, the file wins** — re-read the cited
> file and update this document as part of the change that made it stale. Volatile values
> (versions, pins) are referenced rather than restated.

## What this repository is

The **ViciOne Cluster Editor** is a Blazor **Razor Class Library (RCL)** that provides
an interactive, diagram-based editor for ifm's ViciOne cluster/dataflow model. The
shippable product is the reusable component library `ViciOne.Ui.ClusterEditor`
(`Microsoft.NET.Sdk.Razor`), which is packaged and consumed as a NuGet package by other
applications (e.g. the ViciOne Suite).

The repository also contains a runnable **sample host** (under `samples/`) used for local
development and end-to-end testing.

### Hosting model — IMPORTANT

Although the repository contains a `samples/Client` Blazor **WebAssembly** project, the
sample application is **run as Blazor Server** by default. Treat this as a
**Blazor Server** project unless explicitly told otherwise.

- `samples/Server/Program.cs` chooses the render mode at runtime from the
  `RenderWasm` configuration value.
- `samples/Server/appsettings.json` sets `"RenderWasm": false`, so the default run mode is
  **Interactive Server** (`AddInteractiveServerComponents()` /
  `AddInteractiveServerRenderMode()`).
- The `DesignLoader` hosted service is only registered in Server mode.

The WebAssembly path exists but is not the primary/default configuration.

## Solution structure

Solution file: `ViciOne.Ui.ClusterEditor.slnx`

| Project | Path | SDK / Type | Role |
|---|---|---|---|
| `ViciOne.Ui.ClusterEditor` | `src/ViciOne.Ui.ClusterEditor/` | `Microsoft.NET.Sdk.Razor` | Main shippable RCL — the Cluster Editor component library (packaged as NuGet). |
| `ViciOne.Ui.ColorableIcons` | `src/ViciOne.Ui.ColorableIcons/` | class library | Icon coloring helper library, referenced privately by the main project. |
| `Shared` | `samples/Shared/` | `Microsoft.NET.Sdk.Razor` | Shared sample code (services, extensions, scripts) referencing the main library. |
| `Client` | `samples/Client/` | `Microsoft.NET.Sdk.BlazorWebAssembly` | Blazor WASM sample entry (`App.razor`, `Program.cs`); root namespace `BlazorWasm.Client`. |
| `Server` | `samples/Server/` | `Microsoft.NET.Sdk.Web` | ASP.NET Core host that renders the editor (Server by default, WASM optional). |
| `ViciOne.Ui.ClusterEditor.Tests` | `tests/ViciOne.Ui.ClusterEditor.Tests/` | test | Unit/component tests. |
| `ViciOne.Ui.ColorableIcons.Tests` | `tests/ViciOne.Ui.ColorableIcons.Tests/` | test | Unit tests for the icons library. |
| `ViciOne.Ui.ClusterEditor.EndToEnd.Tests` | `tests/ViciOne.Ui.ClusterEditor.EndToEnd.Tests/` | test | Playwright end-to-end tests. |

## Target framework & build configuration

- **Target framework:**
  `Directory.Build.props` (currently `net10.0`).
- **.NET SDK:** pinned in `global.json` with `rollForward: patch`.
- **Runtime package version pin:** `DotNetVersion` property in `Directory.Packages.props`.
- **Nullable:** enabled; `nullable` warnings are treated as errors (`WarningsAsErrors`).
- **Central Package Management (CPM):** enabled — all package versions live in
  `Directory.Packages.props`. Do **not** add versions to individual `.csproj` files.
- `ViciOne.CodeAnalysis` is a `GlobalPackageReference`, so analyzers apply repo-wide.
  In CI (`$(CI)` set), live analyzers are disabled for performance.
- Test projects use the **Microsoft Testing Platform** runner
  (`UseMicrosoftTestingPlatformRunner`, `global.json` `test.runner`).

## Architecturally significant packages

`Directory.Packages.props` is the **authoritative and exhaustive** list. Only the
dependencies that shape how you write code are called out here:

- **Diagramming:** `Z.Blazor.Diagrams` — the diagram surface the editor is built on.
- **UI components:** DevExpress Blazor is consumed **indirectly** through
  `ViciOne.Ui.Shared.Dx`; prefer `ViciOne.Ui.Blazor.Components` wrappers (e.g. `TextBox`,
  `ComboBox`) over raw `Dx*` components — the changelog shows an ongoing migration in
  that direction.
- **Design tokens:** `ViciOne.Ui.Design` — use its SCSS variables, not legacy/local colors.
- **Domain model:** `ViciOne.Cluster.Model` / `ViciOne.Cluster.Builder` define the
  cluster/dataflow types the editor maps to diagram models.
- **Styling:** `AspNetCore.SassCompiler` compiles `.scss` to `.css` at build.
- **Testing:** xUnit v3 + bUnit + NSubstitute + AwesomeAssertions; Playwright for E2E.

### Private feed — expected failure mode

Several `ViciOne.*` and DevExpress packages come from a private JFrog feed
(`https://system.update.ifm/...`) configured in `NuGet.Config`. Credentials are supplied
locally via .NET user-secrets in `samples/Server` (setup steps in `README.md`).

> **If `dotnet restore` fails with 401/403 on these feeds, the credentials are missing —
> this is an environment problem, not a code problem.** Do **not** "fix" it by removing
> package references, deleting feeds from `NuGet.Config`, downgrading packages, or
> switching to nuget.org. Report the failure and stop.

## Main library layout (`src/ViciOne.Ui.ClusterEditor`)

The library is organized by feature. Blazor components use the code-behind pattern
(`*.razor` + `*.razor.cs`) and colocated scoped styles (`*.razor.scss` compiled to
`*.razor.css`).

- `Components/` — reusable editor components (e.g. `ClusterEditor`, `NodeEditor`,
  `Minimap`, `ContainerEditor`, toolbars, context menus, `UniversalInput`, scrolling).
- `Sections/` — self-contained functional areas, each typically with
  `Components/`, `Services/`, `Models/`, `Extensions/` (and often a
  `ServiceCollectionExtensions` for DI registration): `Dataflow`, `DataPorts`,
  `Topology`, `Property`, `PublishedConnectors`, `Library`, `Information`,
  `File`, `SearchAndTools`.
- `Services/` — core services, including `ClusterServices/` (datastore, projection,
  edit/restructure services) and `ComponentServices/`.
- `Behaviors/` — diagram interaction behaviors (pan, zoom, selection, drag, keyboard).
- `Mappers/DiagramMappers/` — mapping between the cluster model and diagram models.
- `Models/` — DTOs, diagram models, context-menu contexts, comparers.
- `Extensions/`, `Helpers/`, `Builders/`, `Constants/`, `Localization/`, `Resources/`.
- `Styles/` — shared `.scss` (design tokens, mixins) compiled by `AspNetCore.SassCompiler`.
- `Scripts/` — JS interop (`internal/` diagram logic, `external/` third-party like easymde/purify).
- `wwwroot/` — static assets served under `_content/ViciOne.Ui.ClusterEditor/`.

### Dependency injection entry point

`Extensions/IServiceCollectionExtensions.cs` exposes
`AddClusterEditor<TRulesetProvider>(...)` which wires up the editor and its sections
(each section contributes its own registration extension). Hosts call this from their
`Program.cs` (see `samples/Server/Program.cs` and `samples/Client/Program.cs`).

## Localization

- Localization uses `.resx` resources with generated `*.Designer.cs` files (configured
  explicitly in the main `.csproj`).
- Default/neutral language is **en-US**; German (`*.de.resx`) translations are provided
  alongside neutral resources.
- Resources live near their feature (per-component/section `Localization/` folders) plus
  shared resources under `Localization/Resources/`.

## Testing

- **Unit / component:** `tests/ViciOne.Ui.ClusterEditor.Tests` — xUnit v3 + **bUnit**
  (components) + **NSubstitute** + **AwesomeAssertions**. Folders mirror the source structure.
- Internals are visible to the test project via `InternalsVisibleTo`.
- **End-to-end:** `tests/ViciOne.Ui.ClusterEditor.EndToEnd.Tests` — **Playwright**,
  requires one-time local setup (`docs/end-to-end-tests.en.md`).

## Frontend build (npm / SCSS)

The main project's `InstallNpmPackagesAndRunBuildScript` MSBuild target installs npm
packages and runs the npm build automatically in **Debug**. In **Release** you must run
them yourself first (see [Common commands](#common-commands)). SCSS is compiled by
`AspNetCore.SassCompiler` (`sasscompiler.json`).

## Common commands

Run from the solution root unless noted. CI uses the equivalents in `.gitlab-ci.yml`.

```powershell
dotnet restore                                    # requires JFrog credentials (see above)
dotnet build                                      # Debug: npm install + npm build run automatically

npm ci; npm run build; dotnet build -c Release    # Release: frontend build must run FIRST

dotnet run --project samples/Server               # sample host (Blazor Server by default)

dotnet test                                       # all tests
dotnet test tests/ViciOne.Ui.ClusterEditor.Tests  # single project
```

## Git & merge request conventions

- **Branch naming:** When possible, use the gitlab issue number as prefix, otherwise
  just a short description, e.g. 1729-fix-cubic-calculation
- **Commit messages:** no enforced convention — merge requests are **squashed on merge**,
  so the MR title is what ends up in history. Keep individual commit messages short and
  do not spend effort on elaborate formatting; put the meaningful description in the MR
  title and in `CHANGELOG.md`.
- **`CHANGELOG.md` is maintained by hand and must be updated for user-visible changes.**
  Add a bullet under the current `## <version> - Unreleased` heading, in the matching
  `### Added` / `### Changed` subsection. Keep the existing terse, past-tense style
  ("Fixed …", "Refactored …", "Replaced `DxTextBox` with …").
- **Target branch for merge requests:** master
- CI is defined in `.gitlab-ci.yml`; it runs `npm ci` + `npm run-script build` before the
  .NET build, so do not rely on Debug-only automatic npm behavior in pipelines.

## Files you must not hand-edit

These are generated or vendored — change the source, not the output:

- `*.razor.css` — generated from the colocated `*.razor.scss` by `AspNetCore.SassCompiler`.
- `*.Designer.cs` — generated from the corresponding `.resx`.
- `Scripts/external/` — vendored third-party JS (easymde, purify).
- `node_modules/`, `bin/`, `obj/`, and npm build output under `wwwroot/`.
- `NuGet.Config` feed definitions (see the private-feed note above).

## Recipe: adding a new section

Sections are the unit of feature organization. To add one (mirroring the existing
`Dataflow`, `DataPorts`, `Topology`, … sections):

1. Create `src/ViciOne.Ui.ClusterEditor/Sections/<Name>/` with `Components/`,
   `Services/`, `Models/`, `Extensions/` as needed.
2. Add `Extensions/<Name>ServiceCollectionExtensions.cs` exposing an
   `Add<Name>Section(this IServiceCollection services)` method that registers the
   section's services.
3. Call it from `Extensions/IServiceCollectionExtensions.cs` inside
   `AddClusterEditor<TRulesetProvider>` so hosts pick it up automatically.
4. Add components as `*.razor` + `*.razor.cs` (code-behind) with a colocated
   `*.razor.scss` using `ViciOne.Ui.Design` tokens.
5. Add localization: neutral `.resx` plus a `.de.resx` sibling; no hard-coded UI strings.
6. Mirror the folder structure under `tests/ViciOne.Ui.ClusterEditor.Tests/Sections/<Name>/`.
7. Add a `CHANGELOG.md` entry.

## Conventions & guardrails for agents

- **Do not** add package versions to `.csproj` files — use `Directory.Packages.props` (CPM).
- **Do not** change the target framework or SDK versions unless explicitly asked.
- Nullable reference type warnings are build errors — keep code null-safe.
- No hard-coded UI text — add `.resx` entries (neutral + `.de`).
- Treat the app as **Blazor Server** by default (`RenderWasm` = `false`).
- **Do not** call `IJSRuntime` / `IJSObjectReference` invoke or dispose methods directly — use the
  guarded `TryInvoke` / `TryInvokeVoid` / `TryDisposeAsync` extensions in `Extensions/`
  (see `docs/component-guidelines.en.md`).
- Comments on types, members and fields must be XML doc comments (`///`), not `//`. Keep them sparse,
  don't restate the name, and never describe the caller's logic
  (see `docs/code-style-guidelines.en.md` → Comments).

## Useful references

- `README.md` — checkout, credentials, deployment notes.
- `.gitlab-ci.yml` — authoritative build/test/publish steps.

### Guideline docs — read before you touch the related area

These contain the binding rules; this file only summarizes.

| Doc | Read it before… |
|---|---|
| `docs/component-guidelines.en.md` | writing or changing any Blazor component. |
| `docs/code-style-guidelines.en.md` | any C# change — naming, structure, style. |
| `docs/localization-guidelines.en.md` | adding or changing user-facing strings / `.resx`. |
| `docs/end-to-end-tests.en.md` | running or writing Playwright tests (one-time setup). |
| `docs/automation-suite-render-guide.en.md` | rendering/integration work with the Suite. |

## Keeping this document accurate

Update `AGENTS.md` in the same merge request when you change:

- `Directory.Build.props`, `Directory.Packages.props`, `global.json` (TFM/SDK/pins)
- `ViciOne.Ui.ClusterEditor.slnx` (projects added/removed/renamed)
- `samples/Server/appsettings.json` (`RenderWasm` default)
- the `Sections/` set, or the DI entry point signature
