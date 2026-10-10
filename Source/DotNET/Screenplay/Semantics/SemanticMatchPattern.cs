// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.RegularExpressions;

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Holds the portable match rule definition shared by binding, contract validation and execution.
/// </summary>
internal static class SemanticMatchPattern
{
    // Exactly one @; nonempty local part without whitespace or @; at least two nonempty domain labels.
    internal const string Email = @"^[^\s@]+@[^\s@.]+(?:\.[^\s@.]+)+$";

    internal static readonly TimeSpan Timeout = TimeSpan.FromSeconds(1);

    internal static Regex Create(string pattern)
    {
        // Validate the authored text first: callers expose regex error messages and offsets.
        var authored = new Regex(pattern, RegexOptions.ECMAScript | RegexOptions.CultureInvariant, Timeout);
        var rewritten = EndOfInputAnchors(pattern);

        return rewritten == pattern ? authored : new(rewritten, RegexOptions.ECMAScript | RegexOptions.CultureInvariant, Timeout);
    }

    static string EndOfInputAnchors(string pattern)
    {
        var result = new StringBuilder(pattern.Length);
        var classStart = -1;
        for (var index = 0; index < pattern.Length; index++)
        {
            var character = pattern[index];
            if (character == '\\')
            {
                result.Append(character).Append(pattern[++index]);
                continue;
            }

            if (character == '[' && classStart < 0)
            {
                // A leading ']' is literal in a .NET character class; after '^' it closes '[^]', the any-character class.
                classStart = index + 1;
            }
            else if (character == ']' && index > classStart)
            {
                classStart = -1;
            }

            // Unlike .NET '$', this ECMAScript-compatible assertion cannot precede a final newline.
            result.Append(character == '$' && classStart < 0 ? @"(?![\s\S])" : character.ToString());
        }

        return result.ToString();
    }
}
