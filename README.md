# ViciOne Cluster Editor

## Checkout and Development

### Prerequisites

- [Visual Studio](https://visualstudio.microsoft.com/vs/) - version >= 17.x with workload "ASP.NET and web development"
- [nodeJS](https://nodejs.org/en/) version >= 20.x

To run end to end tests locally, an initial setup is required as described here [End to end tests](docs/end-to-end-tests.en.md).

### npm packages

> ⚠ This section is only relevant if you are building with `Release` configuration. If you are building with `Debug` configuration, the required npm packages will be installed and the main npm build script will be run automatically if needed.

Execute the following commands before build in solution root:

- Run `npm i`
- Run `npm run build`

### Package API Credentials

To enable your development system to access these packages, you need to configure credentials so that _Server_ can authenticate against the [JFrog](https://system.update.ifm) API.

Currently, you must manually provide the credentials using .NET UserSecrets.

Please contact the infrastructure team or the repo maintainer to obtain the required `<user>` and `<password>`.

Once you received the password, you can continue to add the credentials by e.g. UserSecrets:
1. Open a powershell and move into the `<<Local Repo Path>>/samples/Server` directory
2. Execute the following commands<br>
```ps
dotnet user-secrets set "ArtifactRepository:Sources:0:UserName" "<user>"
dotnet user-secrets set "ArtifactRepository:Sources:1:UserName" "<user>"
dotnet user-secrets set "ArtifactRepository:Sources:2:UserName" "<user>"

dotnet user-secrets set "ArtifactRepository:Sources:0:Password" "<password>"
dotnet user-secrets set "ArtifactRepository:Sources:1:Password" "<password>"
dotnet user-secrets set "ArtifactRepository:Sources:2:Password" "<password>"
```

## Guidelines

- [Automation Suite Render Guideline](docs/automation-suite-render-guide.de.md)
- [Blazor Component Guideline](docs/component-guidelines.en.md)
- [Code Style Guideline](docs/code-style-guidelines.en.md)
- [CSS Guideline (german)](docs/css-guidelines.de.md)
- [Localization Guideline](docs/localization-guidelines.en.md)

## Deployments

### Hardware

| Name                | Notes                                         |
|---------------------|-----------------------------------------------|
| Demo ec10 1         |                                               |
| Demo ec20 1         |                                               |
| Demo raspi 1        | Only usable for the `master` branch           |

The Cluster Editor log file can be found in `/var/log/vo-cluster-editor.log`.

## Special debug option in release mode

To enable a special debug mode for the `FileSection` in release mode you have to put `debug` to the class
attribute of the div with the class `file-section-container`. The buttons are only valid if the `ClusterEditor`
is published in his own frame app. If the `ClusterEditor` is embedded in the `Suite` the buttons have no effect.
