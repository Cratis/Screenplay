// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Generation;

internal static class RegexCharacters
{
    internal static readonly string Printable = new([.. Enumerable.Range(0x20, 95).Select(value => (char)value)]);

    internal static string Complement(string characters) => new([.. Printable.Where(character => !characters.Contains(character, StringComparison.Ordinal))]);

    internal static string Escape(char character, bool inClass) => character switch
    {
        'd' => "0123456789",
        'D' => Complement("0123456789"),
        'w' => "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789_",
        'W' => Complement("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789_"),
        's' => " ",
        'S' => Complement(" "),
        't' => "\t",
        'n' => "\n",
        'r' => "\r",
        'f' => "\f",
        'v' => "\v",
        'b' or 'B' => throw new OutsideRegexFragment("word boundary"),
        >= '1' and <= '9' => throw new OutsideRegexFragment("backreference"),
        'x' or 'u' or 'c' or '0' => throw new OutsideRegexFragment($"\\{character} escape"),
        _ when "\\^$.|?*+()[]{}".Contains(character, StringComparison.Ordinal) || (inClass && character == '-') => character.ToString(),
        _ => throw new OutsideRegexFragment($"identity escape \\{character}")
    };
}
