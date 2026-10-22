// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using System.Globalization;
using System.Text;

namespace Cratis.Screenplay.Matching;

// RegExpIdentifierName uses Unicode identifiers, even in flagless JavaScript.
// Valid names beyond the portable ASCII spelling are refused, not syntax errors.
internal static class PortableMatchGroupName
{
    static readonly SearchValues<char> _hexDigits = SearchValues.Create("0123456789abcdefABCDEF");
    internal static bool IsJavaScriptName(string name)
    {
        var decoded = new StringBuilder();
        for (var offset = 0; offset < name.Length; offset++)
        {
            if (name[offset] != '\\') decoded.Append(name[offset]);
            else if (!DecodeEscape(name, ref offset, decoded)) return false;
        }
        var first = true;
        foreach (var rune in decoded.ToString().EnumerateRunes())
        {
            if (!IsStart(rune) && (first || !IsContinue(rune))) return false;
            first = false;
        }

        return !first;
    }

    static bool DecodeEscape(string name, ref int offset, StringBuilder decoded)
    {
        if (++offset >= name.Length || name[offset] != 'u') return false;
        var start = ++offset;
        var braced = start < name.Length && name[start] == '{';
        var end = braced ? name.IndexOf('}', ++start) : start + 4;
        if (end < start || end > name.Length || end == start) return false;
        var digits = name.AsSpan(start, end - start);
        if (digits.IndexOfAnyExcept(_hexDigits) >= 0 ||
            !int.TryParse(digits, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var value) || value is < 0 or > 0x10ffff)
        {
            return false;
        }
        if (value <= char.MaxValue) decoded.Append((char)value);
        else decoded.Append(char.ConvertFromUtf32(value));
        offset = braced ? end : end - 1;

        return true;
    }

    static bool IsStart(Rune rune) => rune.Value is '$' or '_' or 0x1885 or 0x1886 or 0x2118 or 0x212e or 0x309b or 0x309c ||
        (rune.Value != 0x2e2f && Rune.GetUnicodeCategory(rune) is UnicodeCategory.UppercaseLetter or UnicodeCategory.LowercaseLetter or
            UnicodeCategory.TitlecaseLetter or UnicodeCategory.ModifierLetter or UnicodeCategory.OtherLetter or UnicodeCategory.LetterNumber);

    static bool IsContinue(Rune rune) => rune.Value is 0x00b7 or 0x0387 or (>= 0x1369 and <= 0x1371) or 0x19da or 0x200c or 0x200d or 0x30fb or 0xff65 ||
        Rune.GetUnicodeCategory(rune) is UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark or UnicodeCategory.DecimalDigitNumber or UnicodeCategory.ConnectorPunctuation;
}
