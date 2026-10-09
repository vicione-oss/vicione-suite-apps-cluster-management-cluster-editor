using System;
using NSubstitute;
using Shared.ClusterSerialization;
using Shared.Extensions;
using ViciOne.Cluster.Builder;
using ViciOne.Cluster.Builder.Abstractions;
using ViciOne.Cluster.Model;
using ViciOne.Ui.ClusterEditor.Tests.Resources;

namespace ViciOne.Ui.ClusterEditor.Tests.TestHelpers;

internal static class BuilderFactory
{
    private static Guid s_fbDesignId;

    internal static Guid FbDesignId => s_fbDesignId;

    internal static IClusterBuilder Create()
    {
        var cluster = new ClusterBuilder(CreateDependencyResolver());
        cluster.AddDemoElements();

        return cluster;
    }

    /// <summary>
    /// A builder over a serialized copy of <paramref name="builder"/>'s cluster, as a save followed by a load
    /// produces it: every element keeps its id but is a new instance.
    /// </summary>
    internal static IClusterBuilder CreateReloaded(IClusterBuilder builder)
    {
        var cluster = ClusterSerializer.Deserialize(ClusterSerializer.Serialize(builder.Cluster));

        return new ClusterBuilder(cluster, CreateDependencyResolver());
    }

    private static IDependencyResolver CreateDependencyResolver()
    {
        var dependencyResolver = Substitute.For<IDependencyResolver>();
        var fbDesign = TestResources.LoadFbDesign();
        s_fbDesignId = fbDesign.Id;
        dependencyResolver.ResolveFunctionBlockDesign(s_fbDesignId).Returns(fbDesign);
        dependencyResolver
            .ResolveFunctionBlockDesignDependency(s_fbDesignId)
            .Returns(new ClusterDependency { Name = "TestDependency", Version = "1.0.0" });
        dependencyResolver
            .ResolveDataPortDesignDependency(Arg.Any<string>())
            .Returns(new ClusterDependency { Name = "DataPortDependency", Version = "1.0.0" });

        return dependencyResolver;
    }
}
