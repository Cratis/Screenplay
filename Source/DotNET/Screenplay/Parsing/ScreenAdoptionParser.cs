// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses <c>uses screen &lt;Name&gt;</c> in a slice and the data the slice provides to the screen's needs.
/// </summary>
internal static partial class ScreenAdoptionParser
{
    /// <summary>
    /// Gets whether a slice line starts an adoption: <c>uses screen ...</c>.
    /// </summary>
    /// <param name="content">The trimmed line content.</param>
    /// <returns><c>true</c> when the line reads <c>uses screen</c>.</returns>
    public static bool IsAdoption(string content) => UsesScreenWordsRegex().IsMatch(content);

    /// <summary>
    /// Parses an adoption from its already consumed header line.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to parse in.</param>
    /// <param name="header">The consumed <see cref="SourceLine"/> holding <c>uses screen &lt;Name&gt;</c>.</param>
    /// <returns>The parsed <see cref="UsesScreenSyntax"/>.</returns>
    public static UsesScreenSyntax Parse(ParserContext context, SourceLine header)
    {
        var match = HeaderRegex().Match(header.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidScreenAdoption, $"Invalid screen adoption '{header.Content}' - expected 'uses screen <Name>'", header.Location);
        }

        var provisions = new List<ScreenProvisionSyntax>();
        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            var provision = ProvisionRegex().Match(line.Content);
            if (!provision.Success)
            {
                context.Error(DiagnosticCodes.InvalidScreenAdoption, $"Invalid provision '{line.Content}' - expected 'data <Need> via query <Query> [by <param>]'", line.Location);
                context.SkipBlock(line.Indent);
                continue;
            }

            provisions.Add(new(provision.Groups[1].Value, provision.Groups[2].Value, provision.Groups[3].Success ? provision.Groups[3].Value : null, line.Location) { Mappings = ParseMappings(context, line) });
        }

        return new(match.Success ? match.Groups[1].Value : string.Empty, header.Location) { Provisions = provisions };
    }

    static List<ScreenProvisionMappingSyntax> ParseMappings(ParserContext context, SourceLine provision)
    {
        var mappings = new List<ScreenProvisionMappingSyntax>();
        while (context.TryPeekChild(provision.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            var mapping = MappingRegex().Match(line.Content);
            if (!mapping.Success)
            {
                context.Error(DiagnosticCodes.InvalidScreenAdoption, $"Invalid mapping '{line.Content}' - expected '<field> from <path>'", line.Location);
                context.SkipBlock(line.Indent);
                continue;
            }

            mappings.Add(new(mapping.Groups[1].Value, mapping.Groups[2].Value, line.Location));
        }

        return mappings;
    }

    [GeneratedRegex(@"^uses\s+screen(\s|$)", RegexOptions.NonBacktracking)]
    private static partial Regex UsesScreenWordsRegex();

    [GeneratedRegex(@"^uses\s+screen\s+([A-Za-z_]\w*)$", RegexOptions.NonBacktracking)]
    private static partial Regex HeaderRegex();

    [GeneratedRegex(@"^data\s+([A-Za-z_]\w*)\s+via\s+query\s+(\w+(?:\.\w+)*)(?:\s+by\s+(\w+))?$", RegexOptions.NonBacktracking)]
    private static partial Regex ProvisionRegex();

    [GeneratedRegex(@"^([A-Za-z_]\w*)\s+from\s+([A-Za-z_]\w*(?:\.[A-Za-z_]\w*)*)$", RegexOptions.NonBacktracking)]
    private static partial Regex MappingRegex();
}
