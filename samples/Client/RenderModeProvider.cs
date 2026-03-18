using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorWasm.Client;

[SuppressMessage("Maintainability", "CA1515:Consider making public types internal", Justification = "Necessary for service initialisation")]
public class RenderModeProvider(bool useWasm = false)
{
    private readonly IComponentRenderMode _contentRenderMode = useWasm ? new InteractiveWebAssemblyRenderMode(prerender: false) : new InteractiveServerRenderMode(prerender: false);
    private readonly IComponentRenderMode _headerRenderMode = useWasm ? new InteractiveWebAssemblyRenderMode(prerender: false) : new InteractiveServerRenderMode(prerender: false);

    public IComponentRenderMode ContentRenderMode => _contentRenderMode;
    public IComponentRenderMode HeaderRenderMode => _headerRenderMode;

    public bool UseWebassembly { get; } = useWasm;
}
