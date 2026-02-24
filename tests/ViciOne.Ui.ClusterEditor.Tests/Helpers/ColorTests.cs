using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using ViciOne.Ui.ClusterEditor.Helpers;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Helpers;

public class ColorTests
{
    [Fact]
    public async Task GetRelativeLuminance_IsThreadSafe_ForManyKeys()
    {
        // Arrange
        // Viele unterschiedliche Keys parallel: sollte deterministisch und fehlerfrei laufen
        var keys = Enumerable.Range(0, 1000).Select(i => $"rgb({i % 256}, {i * 7 % 256}, {i * 13 % 256})").ToArray();
        var bag = new ConcurrentBag<(string key, double value)>();

        // Act
        var tasks = keys.Select(k => Task.Run(() =>
        {
            var v = Color.GetRelativeLuminance(k);
            bag.Add((k, v));
        })).ToArray();

        await Task.WhenAll(tasks);

        // Assert
        // Nach erneutem Zugriff stimmen Werte überein (Cache-Hits, deterministisch)
        foreach (var (key, oldVal) in bag)
        {
            var newVal = Color.GetRelativeLuminance(key);
            Assert.Equal(oldVal, newVal);
        }
    }

    [Fact]
    public async Task GetRelativeLuminance_IsThreadSafe_ForSameKey()
    {
        // Arrange
        const string Rgb = "rgb(10, 20, 30)";
        var results = new double[256];
        var tasks = Enumerable.Range(0, results.Length)
            .Select(i => Task.Run(() => results[i] = Color.GetRelativeLuminance(Rgb)))
            .ToArray();

        // Act
        await Task.WhenAll(tasks);

        // Assert
        // Alle Ergebnisse identisch, keine Exceptions
        var first = results[0];
        Assert.All(results, v => Assert.Equal(first, v));
    }

    [Theory]
    [InlineData("rgb(0, 0, 0)", 0.0)]
    [InlineData("rgb(255, 255, 255)", 1.0)]
    [InlineData("rgb(255, 0, 0)", 0.2126)]
    [InlineData("rgb(0, 255, 0)", 0.7152)]
    [InlineData("rgb(0, 0, 255)", 0.0722)]
    [InlineData("rgb(127, 127, 127)", (0.2126 * 127 / 255) + (0.7152 * 127 / 255) + (0.0722 * 127 / 255))]
    public void GetRelativeLuminance_KnownValues_AreCorrect(string rgb, double expected)
    {
        // Act
        var actual = Color.GetRelativeLuminance(rgb);

        // Assert
        Assert.True(Math.Abs(actual - expected) < 1e-12, $"Expected {expected}, got {actual}");
    }

    [Fact]
    public void GetRelativeLuminance_RepeatedCalls_ReturnSameResult()
    {
        // Arrange
        const string Rgb = "rgb(17, 34, 51)";

        // Act
        var first = Color.GetRelativeLuminance(Rgb);
        var second = Color.GetRelativeLuminance(Rgb);

        // Assert
        Assert.Equal(first, second);
    }
}
