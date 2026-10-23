// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Screenplay.Matching;

internal sealed partial class PortableMatchParser
{
    static string EscapeConstruct(char escape, bool inClass) => escape switch
    {
        (>= '1' and <= '9') or 'k' => "backreference",
        'b' when inClass => "class backspace escape",
        'b' or 'B' => "word boundary",
        'p' or 'P' => "Unicode property escape",
        'u' => "Unicode code point escape",
        'c' => "control escape",
        'A' => "start of input escape",
        'z' or 'Z' => "end of input escape",
        _ => "identity escape"
    };

    MatchCharacterSet CharacterClass(int start)
    {
        var negated = Take('^');
        var result = new MatchCharacterSet();
        if (At(']')) Refuse(negated ? "negated empty character class" : "empty character class", start);
        while (_failure is null && _offset < pattern.Length && !At(']'))
        {
            if (At('-') && _offset + 1 < pattern.Length && pattern[_offset + 1] == '[') Refuse("class subtraction", _offset);
            var left = ClassItem();
            if (At('-') && _offset + 1 < pattern.Length && pattern[_offset + 1] != ']')
            {
                var rangeOffset = _offset++;
                if (At('[')) Refuse("class subtraction", rangeOffset);
                var right = ClassItem();
                if (left.Single is not { } first || right.Single is not { } last) Refuse("class escape range endpoint", rangeOffset);
                else if (first > last) Invalid("reversed character range", rangeOffset);
                else left = MatchCharacterSet.Range(first, last);
            }
            result.Union(left);
        }
        if (!Take(']')) Invalid("unterminated character class", start);
        if (negated) result.Complement();

        return result;
    }

    MatchCharacterSet ClassItem()
    {
        if (_offset >= pattern.Length)
        {
            Invalid("unterminated character class", _offset);
            return new();
        }
        var start = _offset;
        var character = pattern[_offset++];

        return character == '\\' ? Escape(start, true) : MatchCharacterSet.Character(character);
    }

    MatchCharacterSet Escape(int start, bool inClass)
    {
        if (_offset == pattern.Length)
        {
            Invalid("trailing backslash", start);
            return new();
        }
        var escape = pattern[_offset++];
        if (escape is 'd' or 'D' or 'w' or 'W' or 's' or 'S') return MatchCharacterSet.Escape(escape);
        var literal = escape switch
        {
            't' => '\t', 'n' => '\n', 'r' => '\r', 'f' => '\f', 'v' => '\v', '0' => '\0', _ => escape
        };
        if (escape == '0' && _offset < pattern.Length && char.IsAsciiDigit(pattern[_offset])) Refuse("octal escape", start);
        else if (escape is 't' or 'n' or 'r' or 'f' or 'v' or '0') return MatchCharacterSet.Character(literal);
        else if (escape == 'x' || (escape == 'u' && !At('{'))) return Hexadecimal(start, escape == 'x' ? 2 : 4);
        else if ("^$\\.*+?()[]{}|/".Contains(escape, StringComparison.Ordinal) || (inClass && escape == '-')) return MatchCharacterSet.Character(escape);
        else Refuse(EscapeConstruct(escape, inClass), start);

        return new();
    }

    MatchCharacterSet Hexadecimal(int start, int width)
    {
        if (_offset + width > pattern.Length || !ushort.TryParse(pattern.AsSpan(_offset, Math.Min(width, pattern.Length - _offset)), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var code))
        {
            Invalid("invalid hexadecimal escape", start);
            return new();
        }
        _offset += width;

        return MatchCharacterSet.Character((char)code);
    }
}
