// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Checks calendar and offset spelling only for response and generated-fixture compatibility.
/// </summary>
internal static partial class ResponseDateValues
{
    internal static bool Compatible(string text, bool includeTime)
    {
        var match = (includeTime ? DateTimeRegex() : DateRegex()).Match(text);
        if (!match.Success) return false;
        var year = Part(match, 1);
        var month = Part(match, 2);
        var day = Part(match, 3);
        if (year < 1 || month < 1 || month > 12 || day < 1 || day > DateTime.DaysInMonth(year, month)) return false;
        if (!includeTime) return true;
        if (Part(match, 4) > 23 || Part(match, 5) > 59 || Part(match, 6) > 59) return false;
        if (!match.Groups[7].Success) return true;
        var hours = Part(match, 7);
        var minutes = Part(match, 8);

        return hours <= 14 && minutes <= 59 && (hours != 14 || minutes == 0);
    }

    static int Part(Match match, int group) => int.Parse(match.Groups[group].Value, CultureInfo.InvariantCulture);

    [GeneratedRegex(@"\A([0-9]{4})-([0-9]{2})-([0-9]{2})\z", RegexOptions.None, 1000)]
    private static partial Regex DateRegex();

    [GeneratedRegex(@"\A([0-9]{4})-([0-9]{2})-([0-9]{2})T([0-9]{2}):([0-9]{2}):([0-9]{2})(?:\.[0-9]{1,7})?(?:Z|[+-]([0-9]{2}):([0-9]{2}))\z", RegexOptions.None, 1000)]
    private static partial Regex DateTimeRegex();
}
