// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.RegularExpressions;

namespace Cratis.Screenplay.Strings;

/// <summary>
/// Represents the content of a <c>.strings</c> file - localized strings keyed by dotted paths.
/// </summary>
/// <remarks>
/// The format is line based: <c>&lt;key.path&gt; = "&lt;value&gt;"</c> assignments with <c>//</c> comments
/// and blank lines allowed between them. Values are standard double-quoted strings and keep
/// <c>{placeholder}</c> tokens verbatim for the consumer to substitute.
/// </remarks>
/// <param name="entries">The <see cref="StringsEntry">entries</see> of the file.</param>
public partial class StringsFile(IEnumerable<StringsEntry> entries)
{
    /// <summary>
    /// Gets the <see cref="StringsEntry">entries</see> of the file, in declaration order.
    /// </summary>
    public IEnumerable<StringsEntry> Entries { get; } = [.. entries];

    /// <summary>
    /// Parses the content of a <c>.strings</c> file.
    /// </summary>
    /// <param name="content">The content to parse.</param>
    /// <returns>The parsed <see cref="StringsFile"/>.</returns>
    /// <exception cref="InvalidStringsLine">A line is not a comment, blank or a valid string assignment.</exception>
    public static StringsFile Parse(string content)
    {
        var entries = new List<StringsEntry>();
        var lines = content.Split('\n');
        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index].TrimEnd('\r').Trim();
            if (line.Length == 0 || line.StartsWith("//", StringComparison.Ordinal))
            {
                continue;
            }

            var match = EntryRegex().Match(line);
            if (!match.Success)
            {
                throw new InvalidStringsLine(index + 1, line);
            }

            entries.Add(new(match.Groups[1].Value, match.Groups[2].Value));
        }

        return new(entries);
    }

    /// <summary>
    /// Writes the file back out in its canonical form - assignments aligned on the longest key.
    /// </summary>
    /// <returns>The written content.</returns>
    public string Write()
    {
        var width = Entries.Any() ? Entries.Max(entry => entry.Key.Length) : 0;
        var builder = new StringBuilder();
        foreach (var entry in Entries)
        {
            builder.Append(entry.Key.PadRight(width)).Append(" = \"").Append(entry.Value).Append("\"\n");
        }

        return builder.ToString();
    }

    [GeneratedRegex(@"^([A-Za-z_]\w*(?:\.\w+)*)\s*=\s*""([^""]*)""$", RegexOptions.None, 1000)]
    private static partial Regex EntryRegex();
}
