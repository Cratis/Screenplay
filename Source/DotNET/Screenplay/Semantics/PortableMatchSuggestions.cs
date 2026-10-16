// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

// Kept equal to the TypeScript table by the shared portable-match conformance vectors.
internal static class PortableMatchSuggestions
{
    internal static string For(string construct) => construct switch
    {
        "lookaround" => "There is no general equivalent; a length-only lookahead can be replaced with a {n} counted repeat.",
        "backreference" => "There is no equivalent in the portable match dialect.",
        "word boundary" => "There is no equivalent word-boundary assertion in the portable match dialect.",
        "Unicode property escape" => "Use an explicit character class where the required set is known; there is no general equivalent.",
        "Unicode code point escape" => "Use \\uHHHH escapes for UTF-16 code units.",
        "control escape" => "Use an explicit hexadecimal escape such as \\x01.",
        "class backspace escape" => "Use \\x08.",
        "start of input escape" => "Use ^.",
        "end of input escape" => "Use $.",
        "inline options" => "Use explicit case classes such as [aA] for case-insensitive matching; there is no general equivalent for other options.",
        "empty character class" => "Use [^\\s\\S] to match no UTF-16 code unit.",
        "negated empty character class" => "Use [\\s\\S].",
        "class subtraction" => "Use an explicit character class containing the resulting ranges.",
        "class escape range endpoint" => "Write single-character range endpoints or a union of classes.",
        "unescaped closing bracket" => "Escape a literal closing bracket as \\].",
        "unescaped brace" => "Escape a literal brace as \\{ or \\}.",
        "identity escape" => "Write the literal character without a backslash, or use \\xHH or \\uHHHH.",
        "octal escape" => "Use \\xHH or \\uHHHH.",
        "group name" => "Use a name matching [A-Za-z_][A-Za-z0-9_]*.",
        "group extension" => "Use an ordinary group or a (?:...) non-capturing group.",
        "pattern length" => "Shorten the pattern to at most 1000 UTF-16 code units.",
        "expanded pattern size" => "Reduce repeats and alternatives to at most 3000 expanded nodes, including choice points.",
        _ => "Correct the regular expression syntax."
    };
}
