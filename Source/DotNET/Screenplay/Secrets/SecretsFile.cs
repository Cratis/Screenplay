// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.RegularExpressions;

namespace Cratis.Screenplay.Secrets;

/// <summary>
/// Represents the content of a <c>.secrets</c> file - named encrypted values, one per line.
/// </summary>
/// <remarks>
/// The format is line based and human readable: <c>&lt;name&gt; = enc:v1:&lt;base64&gt;</c> assignments
/// with <c>//</c> comments and blank lines allowed between them. Values are always encrypted - the
/// file never holds plaintext secrets.
/// </remarks>
/// <param name="entries">The <see cref="SecretsEntry">entries</see> of the file.</param>
public partial class SecretsFile(IEnumerable<SecretsEntry> entries)
{
    /// <summary>
    /// Gets the <see cref="SecretsEntry">entries</see> of the file, in declaration order.
    /// </summary>
    public IEnumerable<SecretsEntry> Entries { get; } = [.. entries];

    /// <summary>
    /// Parses the content of a <c>.secrets</c> file.
    /// </summary>
    /// <param name="content">The content to parse.</param>
    /// <returns>The parsed <see cref="SecretsFile"/>.</returns>
    /// <exception cref="InvalidSecretsLine">A line is not a comment, blank or a valid secret assignment.</exception>
    public static SecretsFile Parse(string content)
    {
        var entries = new List<SecretsEntry>();
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
                throw new InvalidSecretsLine(index + 1, line);
            }

            entries.Add(new(match.Groups[1].Value, match.Groups[2].Value));
        }

        return new(entries);
    }

    /// <summary>
    /// Writes the file back out in its canonical form - assignments aligned on the longest name.
    /// </summary>
    /// <returns>The written content.</returns>
    public string Write()
    {
        var width = Entries.Any() ? Entries.Max(entry => entry.Name.Length) : 0;
        var builder = new StringBuilder();
        foreach (var entry in Entries)
        {
            builder.Append(entry.Name.PadRight(width)).Append(" = ").Append(entry.EncryptedValue).Append('\n');
        }

        return builder.ToString();
    }

    [GeneratedRegex(@"^([A-Za-z_][\w.]*)\s*=\s*(enc:\w+:\S+)$", RegexOptions.None, 1000)]
    private static partial Regex EntryRegex();
}
