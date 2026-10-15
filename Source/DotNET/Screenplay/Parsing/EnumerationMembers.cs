// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Parsing;

internal sealed partial class EnumerationMembers
{
    readonly List<int> _numbers = [];
    readonly HashSet<int> _seen = [];
    int _numbered;

    internal bool TryNumbered(ParserContext context, SourceLine line, out string name)
    {
        var match = MemberRegex().Match(line.Content);
        name = LineText.Unescape(match.Groups[1].Value);
        if (!match.Success) return false;
        _numbered++;
        var literal = match.Groups[2].Value;
        if (!NumberRegex().IsMatch(literal))
        {
            context.Error(DiagnosticCodes.MalformedEnumerationNumber, $"Invalid enum number '{literal}' - expected an optional '-' followed by decimal digits, with no leading zero", line.Location);
        }
        else if (!int.TryParse(literal, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var number))
        {
            context.Error(DiagnosticCodes.EnumerationNumberOutsideInt32, $"Enum number '{literal}' is outside the int32 range", line.Location);
        }
        else
        {
            _numbers.Add(number);
            if (!_seen.Add(number)) context.Error(DiagnosticCodes.DuplicateEnumerationNumber, $"Enum number '{number.ToString(CultureInfo.InvariantCulture)}' is declared more than once", line.Location);
        }

        return true;
    }

    internal IEnumerable<int>? Finish(ParserContext context, SourceLine declaration, int count)
    {
        if (_numbered > 0 && _numbered != count)
        {
            context.Error(DiagnosticCodes.MixedEnumerationNumbering, "Enum members must either all declare numbers or all omit them", declaration.Location);
        }

        return _numbered > 0 && _numbers.Count == count ? _numbers : null;
    }

    [GeneratedRegex(@"^(@?[a-z_]\w*)\s*=\s*(.*)$", RegexOptions.NonBacktracking)]
    private static partial Regex MemberRegex();

    [GeneratedRegex(@"^-?(0|[1-9][0-9]*)\z", RegexOptions.NonBacktracking)]
    private static partial Regex NumberRegex();
}
