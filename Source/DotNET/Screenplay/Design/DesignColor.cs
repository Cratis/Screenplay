// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;

namespace Cratis.Screenplay.Design;

/// <summary>
/// Reads the color literals a theme may hold (<c>#rgb</c>, <c>#rrggbb</c>, <c>#rrggbbaa</c>, <c>rgb(r, g, b)</c>) and measures contrast between two of them.
/// </summary>
public static partial class DesignColor
{
    /// <summary>
    /// Tries to read a color literal.
    /// </summary>
    /// <param name="value">The literal text.</param>
    /// <param name="red">The red channel, 0 to 255.</param>
    /// <param name="green">The green channel, 0 to 255.</param>
    /// <param name="blue">The blue channel, 0 to 255.</param>
    /// <returns><see langword="true"/> when the text is a color literal; the alpha channel, when present, is ignored.</returns>
    public static bool TryParse(string value, out int red, out int green, out int blue)
    {
        red = green = blue = 0;
        var text = value.Trim();
        var hex = HexRegex().Match(text);
        if (hex.Success)
        {
            var digits = hex.Groups[1].Value;
            if (digits.Length is 3 or 4)
            {
                digits = string.Concat(digits.Take(3).Select(digit => new string(digit, 2)));
            }

            red = int.Parse(digits.AsSpan(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            green = int.Parse(digits.AsSpan(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            blue = int.Parse(digits.AsSpan(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            return true;
        }

        var rgb = RgbRegex().Match(text);
        if (!rgb.Success)
        {
            return false;
        }

        red = int.Parse(rgb.Groups[1].Value, CultureInfo.InvariantCulture);
        green = int.Parse(rgb.Groups[2].Value, CultureInfo.InvariantCulture);
        blue = int.Parse(rgb.Groups[3].Value, CultureInfo.InvariantCulture);
        return red <= 255 && green <= 255 && blue <= 255;
    }

    /// <summary>
    /// Measures the WCAG contrast ratio between two color literals.
    /// </summary>
    /// <param name="foreground">The foreground color literal.</param>
    /// <param name="background">The background color literal.</param>
    /// <param name="ratio">The ratio, from 1 to 21.</param>
    /// <returns><see langword="true"/> when both texts are color literals.</returns>
    public static bool TryContrast(string foreground, string background, out double ratio)
    {
        ratio = 0;
        if (!TryParse(foreground, out var fr, out var fg, out var fb) || !TryParse(background, out var br, out var bg, out var bb))
        {
            return false;
        }

        var first = Luminance(fr, fg, fb);
        var second = Luminance(br, bg, bb);
        var (lighter, darker) = first >= second ? (first, second) : (second, first);
        ratio = (lighter + 0.05) / (darker + 0.05);
        return true;
    }

    static double Luminance(int red, int green, int blue) => (0.2126 * Channel(red)) + (0.7152 * Channel(green)) + (0.0722 * Channel(blue));

    static double Channel(int value)
    {
        var scaled = value / 255.0;
        return scaled <= 0.03928 ? scaled / 12.92 : Math.Pow((scaled + 0.055) / 1.055, 2.4);
    }

    [GeneratedRegex("^#([0-9a-fA-F]{8}|[0-9a-fA-F]{6}|[0-9a-fA-F]{4}|[0-9a-fA-F]{3})$", RegexOptions.NonBacktracking)]
    private static partial Regex HexRegex();

    [GeneratedRegex(@"^rgba?\(\s*(\d{1,3})\s*,\s*(\d{1,3})\s*,\s*(\d{1,3})\s*(?:,\s*[\d.]+\s*)?\)$", RegexOptions.NonBacktracking)]
    private static partial Regex RgbRegex();
}
