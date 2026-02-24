using Xunit;

namespace ViciOne.Ui.ClusterEditor.EndToEnd.Tests.Infrastructure;

[CollectionDefinition(Name)]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "Collection is a xunit term here")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Maintainability", "CA1515:Consider making public types internal", Justification = "xUnit1027: Collection definition classes must be public. Add or change the visibility modifier of the collection definition class to public.")]
public class TestWebApplicationFactoryCollection : ICollectionFixture<TestWebApplicationFactory>
{
    // This class has no code, and is never created. Its purpose is simply to be the place
    // to apply [CollectionDefinition] and all the ICollectionFixture<> interfaces.

    // see https://xunit.net/docs/shared-context

    public const string Name = "WebApplicationFactory collection";
}
