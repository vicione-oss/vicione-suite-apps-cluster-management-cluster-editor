using System;
using System.Collections;
using System.Collections.Generic;

namespace ViciOne.Ui.ClusterEditor.Models.Comparer;

/// <summary>
/// Stellt einen alphanumerischen Zeichenfolgenvergleichsvorgang dar.
/// </summary>
public sealed class AlphaNumericComparer : IComparer
{
    /// <summary>
    /// Gibt die Instanz des <see cref="AlphaNumericComparer"/>s zurück.
    /// </summary>
    public static AlphaNumericComparer Default { get; } = new AlphaNumericComparer();

    private AlphaNumericComparer() { }

    /// <summary>
    /// Vergleicht zwei Objekte oder Zeichenfolgen und gibt eine Angabe der relativen Sortierreihenfolge zurück anhand eines alphanumerischen Vergleichs.
    /// </summary>
    /// <param name="x">Ein mit <paramref name="y"/> zu vergleichendes Objekt.</param>
    /// <param name="y">Ein mit <paramref name="x"/> zu vergleichendes Objekt.</param>
    /// <returns>Eine ganze Zahl mit Vorzeichen, die die relativen Werte von <paramref name="x"/> und <paramref name="y"/> angibt.</returns>
    public int Compare(object? x, object? y)
    {
        if (x is null)
        {
            if (y is null)
                return 0;
            else
                return -1;
        }
        else
        {
            if (y is null)
                return 1;
        }

        var s1 = x.ToString();
        var s2 = y.ToString();

        if (x is DictionaryEntry entry1)
            s1 = entry1.Key.ToString();

        if (y is DictionaryEntry entry2)
            s2 = entry2.Key.ToString();

        if (string.IsNullOrEmpty(s1))
        {
            if (string.IsNullOrEmpty(s2))
                return 0;
            else
                return -1;
        }
        else
        {
            if (string.IsNullOrEmpty(s2))
                return 1;
        }

        var length1 = s1.Length;
        var length2 = s2.Length;

        var marker1 = 0;
        var marker2 = 0;

        char char1;
        char char2;

        int index1;
        int index2;

        char[] space1;
        char[] space2;

        // Durchlaufen der Zeichenketten mit 2 Markern
        while (marker1 < length1 && marker2 < length2)
        {
            char1 = s1[marker1];
            char2 = s2[marker2];

            // Puffer und Variablen anlegen
            index1 = 0;
            index2 = 0;

            space1 = new char[length1];
            space2 = new char[length2];

            // Durchlaufen der Zeichenketten und heraussuchen aller Zahlen
            do
            {
                space1[index1++] = char1;
                marker1++;

                if (marker1 < length1)
                    char1 = s1[marker1];
                else
                    break;
            } while (char.IsDigit(char1) == char.IsDigit(space1[0]));

            do
            {
                space2[index2++] = char2;
                marker2++;

                if (marker2 < length2)
                    char2 = s2[marker2];
                else
                    break;
            } while (char.IsDigit(char2) == char.IsDigit(space2[0]));

            // Erzeugen der Strings, wenn diese mit Zahlen beginnen, dann numerisch vergleichen,
            // ansonsten alphanumerisch
            int result;
            if (char.IsDigit(space1[0]) && char.IsDigit(space2[0]))
            {
                _ = long.TryParse(new string(space1), out var long1);
                _ = long.TryParse(new string(space2), out var long2);

                result = long1.CompareTo(long2);
            }
            else
            {
                result = string.Compare(new string(space1), new string(space2), StringComparison.OrdinalIgnoreCase);
            }

            if (result != 0)
                return result;
        }

        return length1 - length2;
    }
}

/// <summary>
/// Stellt einen alphanumerischen Zeichenfolgenvergleichsvorgang dar.
/// </summary>
public sealed class AlphaNumericComparer<T> : IComparer<T>
{
    private static readonly AlphaNumericComparer s_comparer = AlphaNumericComparer.Default;

    /// <summary>
    /// Gibt für den vom generischen Argument angegebenen Typ einen alphanumerischen Standardvergleich für die Sortierreihenfolgen zurück.
    /// </summary>
    public static AlphaNumericComparer<T> Default { get; } = new AlphaNumericComparer<T>();

    private AlphaNumericComparer() { }

    /// <summary>
    /// Vergleicht zwei Objekte oder Zeichenfolgen und gibt eine Angabe der relativen Sortierreihenfolge zurück anhand eines alphanumerischen Vergleichs.
    /// </summary>
    /// <param name="x">Ein mit <paramref name="y"/> zu vergleichendes Objekt.</param>
    /// <param name="y">Ein mit <paramref name="x"/> zu vergleichendes Objekt.</param>
    /// <returns>Eine ganze Zahl mit Vorzeichen, die die relativen Werte von <paramref name="x"/> und <paramref name="y"/> angibt.</returns>
    public int Compare(T? x, T? y) => s_comparer.Compare(x, y);
}
