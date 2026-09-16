# End to end tests

This document describes the usage of end to end tests with the Playwright framework in the Cluster Editor.
Playwright is used together with XUnit and a `WebApplicationFactory` to run the tests locally.

The framework code is inspired by https://github.com/martincostello/dotnet-minimal-api-integration-testing/
as well as https://danieldonbavand.com/2022/06/13/using-playwright-with-the-webapplicationfactory-to-test-a-blazor-application/

## Setup

To run these tests, a first time setup is necessary (see https://playwright.dev/dotnet/docs/library):
  - install required browsers, replace netX with actual output folder name, e.g. net10.0.
         `pwsh bin/Debug/netX/playwright.ps1 install`
  - if the pwsh command does not work (throws TypeNotFound), make sure to use an up-to-date version of PowerShell.
         `dotnet tool update --global PowerShell`

## Running the tests

After the inital setup, the tests can be run as usual via the Test Explorer (group by Traits, then look for "Category \[Playwright\]").
When the tests are run in `Debug` mode, the actions of the test should be visible (Headless = false, SlowMo = 1000), otherwise
the tests will be run in headless mode.
