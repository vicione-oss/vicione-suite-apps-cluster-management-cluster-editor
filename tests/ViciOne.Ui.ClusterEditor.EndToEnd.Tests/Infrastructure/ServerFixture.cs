using ViciOne.Ui.Testing.Playwright.Infrastructure;

namespace ViciOne.Ui.ClusterEditor.EndToEnd.Tests.Infrastructure;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Maintainability", "CA1515:Consider making public types internal", Justification = "Needed because of required public constructor.")]
public sealed class ServerFixture : WebApplicationFixture<Program>;
