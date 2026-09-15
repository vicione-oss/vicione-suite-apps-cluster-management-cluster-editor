using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using ViciOne.Core.Dataflow.DataModel;
using ViciOne.Ui.ClusterEditor.Sections.Library.Services;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Sections.Library.Services;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Used in testing")]
internal sealed class LibraryServiceTests
{
    public sealed class FunctionBlockCreationRequestedEvent
    {
        private readonly FakeLogger<LibraryService> _logger = new();

        [Fact]
        public async Task RequestFunctionBlockCreation_AwaitsAllSubscribers()
        {
            var sut = new LibraryService(_logger);
            var designId = Guid.NewGuid();
            var first = false;
            var second = false;
            sut.FunctionBlockCreationRequested += id =>
            {
                first = id == designId;
                return Task.CompletedTask;
            };
            sut.FunctionBlockCreationRequested += _ =>
            {
                second = true;
                return Task.CompletedTask;
            };

            await sut.RequestFunctionBlockCreation(designId);

            first.Should().BeTrue();
            second.Should().BeTrue();
        }

        [Fact]
        public async Task RequestFunctionBlockCreation_WhenSubscriberThrows_IsolatesAndLogs()
        {
            var sut = new LibraryService(_logger);
            var second = false;
            sut.FunctionBlockCreationRequested += _ => throw new InvalidOperationException("x");
            sut.FunctionBlockCreationRequested += _ =>
            {
                second = true;
                return Task.CompletedTask;
            };

            await sut.RequestFunctionBlockCreation(Guid.NewGuid());

            second.Should().BeTrue();
            _logger.Collector.GetSnapshot().Count(l => l.Level == LogLevel.Error).Should().Be(1);
        }
    }

    public sealed class CreateLibraryEntries
    {
        private readonly LibraryService _libraryService = new(new FakeLogger<LibraryService>());

        private static FunctionBlockDesign CreateDesign(string designNamespace, string designName)
            => new()
            {
                Name = designName,
                Namespace = designNamespace
            };

        [Fact]
        public void Groups_namespaces()
        {
            _libraryService.CreateLibraryEntries(
                [
                    CreateDesign(string.Empty, "Name 1"),
                    CreateDesign("Namespace a", "Name a1"),
                    CreateDesign("Namespace b", "Name b1"),
                    CreateDesign(string.Empty, "Name 2"),
                    CreateDesign("Namespace a", "Name a2"),
                    CreateDesign("Namespace b", "Name b2"),
                ]);
            var result = _libraryService.LibraryEntries.ToArray();

            Assert.Equal(4, result.Length);

            Assert.Equal("Namespace a", result.ElementAt(0).Name);
            Assert.Equal(2, result.ElementAt(0).Children.Count);
            Assert.Equal("Name a1", result.ElementAt(0).Children[0].Name);
            Assert.Equal("Name a2", result.ElementAt(0).Children[1].Name);

            Assert.Equal("Namespace b", result.ElementAt(1).Name);
            Assert.Equal(2, result.ElementAt(1).Children.Count);
            Assert.Equal("Name b1", result.ElementAt(1).Children[0].Name);
            Assert.Equal("Name b2", result.ElementAt(1).Children[1].Name);

            Assert.Equal("Name 1", result.ElementAt(2).Name);
            Assert.Equal("Name 2", result.ElementAt(3).Name);
        }

        [Fact]
        public void Reads_empty_input()
        {
            _libraryService.CreateLibraryEntries([]);
            var result = _libraryService.LibraryEntries.ToArray();

            Assert.Empty(result);
        }

        [Fact]
        public void Reads_empty_namespace()
        {
            _libraryService.CreateLibraryEntries(
                [
                    CreateDesign(string.Empty, "Name 1"),
                    CreateDesign(string.Empty, "Name 2")
                ]);
            var result = _libraryService.LibraryEntries.ToArray();

            Assert.Equal(2, result.Length);

            Assert.Equal("Name 1", result.ElementAt(0).Name);
            Assert.Equal("Name 2", result.ElementAt(1).Name);
        }

        [Fact]
        public void Reads_nested_namespaces()
        {
            _libraryService.CreateLibraryEntries(
                [
                    CreateDesign("Namespace a.Namespace b.Namespace c", "Name abc1"),
                    CreateDesign("Namespace a.Namespace b.Namespace c", "Name abc2"),
                    CreateDesign("Namespace a.Namespace b.Namespace d", "Name abd1"),
                ]);
            var result = _libraryService.LibraryEntries.ToArray();

            var entry_1 = Assert.Single(result);

            Assert.Equal("Namespace a", entry_1.Name);
            var entry_2 = Assert.Single(entry_1.Children);

            Assert.Equal("Namespace b", entry_2.Name);
            Assert.Equal(2, entry_2.Children.Count);

            Assert.Equal("Namespace c", entry_2.Children[0].Name);
            Assert.Equal(2, entry_2.Children[0].Children.Count);
            Assert.Equal("Name abc1", entry_2.Children[0].Children[0].Name);
            Assert.Equal("Name abc2", entry_2.Children[0].Children[1].Name);

            Assert.Equal("Namespace d", entry_2.Children[1].Name);
            var item = Assert.Single(entry_2.Children[1].Children);
            Assert.Equal("Name abd1", item.Name);
        }

        [Fact]
        public void Reads_same_name_and_namespace()
        {
            _libraryService.CreateLibraryEntries(
                [
                    CreateDesign("1", "1"),
                    CreateDesign("1", "2"),
                    CreateDesign("2", "2"),
                ]);
            var result = _libraryService.LibraryEntries.ToArray();

            Assert.Equal(2, result.Length);

            Assert.Equal("1", result.ElementAt(0).Name);
            Assert.Equal(2, result.ElementAt(0).Children.Count);
            Assert.Equal("1", result.ElementAt(0).Children[0].Name);
            Assert.Equal("2", result.ElementAt(0).Children[1].Name);

            Assert.Equal("2", result.ElementAt(1).Name);
            var item = Assert.Single(result.ElementAt(1).Children);
            Assert.Equal("2", item.Name);
        }
    }
}
