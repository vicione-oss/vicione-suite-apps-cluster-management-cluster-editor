using System;
using NSubstitute;
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
        var dependencyResolver = Substitute.For<IDependencyResolver>();
        var fbDesign = TestResources.LoadFbDesign();
        s_fbDesignId = fbDesign.Id;
        dependencyResolver.ResolveFunctionBlockDesign(s_fbDesignId).Returns(fbDesign);
        dependencyResolver
            .ResolveFunctionBlockDesignDependency(s_fbDesignId)
            .Returns(new ClusterDependency { Name = "TestDependency", Version = "1.0.0" });

        var cluster = new ClusterBuilder(dependencyResolver);
        cluster.AddDemoElements();

        return cluster;
    }
}
