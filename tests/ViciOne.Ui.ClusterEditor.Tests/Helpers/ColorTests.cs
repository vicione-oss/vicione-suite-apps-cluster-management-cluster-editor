using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using ViciOne.Ui.ClusterEditor.Helpers;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Helpers;

public class ColorTests
{
    private const string Fallback = "rgb(0, 0, 0)";

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

    [Theory]
    [InlineData("#fff")]
    [InlineData("#FFFA")]
    [InlineData("#a1b2c3")]
    [InlineData("#ff000080")]
    [InlineData("rgb(149, 149, 149)")]
    [InlineData("RGB(149,149,149)")]
    [InlineData("rgb(100%, 0%, 50%)")]
    [InlineData("rgba(1, 2, 3, 0.5)")]
    [InlineData("rgb(1 2 3)")]
    [InlineData("rgb(1 2 3 / 50%)")]
    [InlineData("hsl(120, 50%, 50%)")]
    [InlineData("hsla(120deg, 50%, 50%, .5)")]
    [InlineData("hsl(0.5turn 50% 50% / 0.25)")]
    [InlineData("red")]
    [InlineData("RebeccaPurple")]
    [InlineData("transparent")]
    [InlineData(" #fff ")]
    public void SanitizeCssColor_ValidColor_ReturnsValue(string value)
    {
        // Act
        var actual = Color.SanitizeCssColor(value, Fallback);

        // Assert
        Assert.True(Color.IsValidCssColor(value));
        Assert.Equal(value, actual);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("red; display:none")]
    [InlineData("red} body{display:none")]
    [InlineData("url(https://example.com/x.png)")]
    [InlineData("\"red\"")]
    [InlineData("#GGG")]
    [InlineData("#12345")]
    [InlineData("#")]
    [InlineData("notacolor")]
    [InlineData("💥")]
    [InlineData("rgb(")]
    [InlineData("rgb(1, 2)")]
    [InlineData("rgb(1, 2, 3, 4, 5)")]
    [InlineData("rgb(1 2 3 4)")]
    [InlineData("rgb(1, 2 3)")]
    [InlineData("rgb(a, b, c)")]
    [InlineData("cmyk(1, 2, 3, 4)")]
    [InlineData("red blue")]
    public void SanitizeCssColor_InvalidValue_ReturnsFallback(string? value)
    {
        // Act
        var actual = Color.SanitizeCssColor(value, Fallback);

        // Assert
        Assert.False(Color.IsValidCssColor(value));
        Assert.Equal(Fallback, actual);
    }
}
