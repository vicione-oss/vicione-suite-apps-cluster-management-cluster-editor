using System.Diagnostics.CodeAnalysis;
using Sdk.Backend.Modules;

namespace Server.Designs;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Used in testing")]
internal sealed class FakeBackendModule : BackendModule
{ }
