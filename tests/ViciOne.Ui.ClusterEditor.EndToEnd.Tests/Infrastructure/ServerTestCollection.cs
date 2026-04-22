using ViciOne.Ui.Testing.Playwright.Attributes;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.EndToEnd.Tests.Infrastructure;

/// <summary>
/// This class has no code, and is never created. Its purpose is simply to be the place
/// to apply <see cref="CollectionDefinitionAttribute"/> and all the <see cref="ICollectionFixture{TFixture}"/> interfaces.
/// </summary>
/// <remarks>
/// <see href="https://xunit.net/docs/shared-context#collection-fixture"/>
/// </remarks>
[CollectionDefinition("Server web application test collection")]
[PlaywrightTest]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "Collection is a xunit term here")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Maintainability", "CA1515:Consider making public types internal", Justification = "xUnit1027: Collection definition classes must be public. Add or change the visibility modifier of the collection definition class to public.")]
public class ServerTestCollection : ICollectionFixture<ServerFixture>;
