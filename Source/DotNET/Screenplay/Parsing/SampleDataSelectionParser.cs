// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses the <c>sample data</c> block of a <c>ui profile</c>: the one place the whole application's active pack is chosen.
/// </summary>
internal static partial class SampleDataSelectionParser
{
    /// <summary>
    /// Parses a profile's sample data block whose header line has just been consumed.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    /// <param name="header">The <c>sample data</c> line inside the profile.</param>
    /// <returns>The parsed <see cref="SampleDataSelectionSyntax"/>.</returns>
    public static SampleDataSelectionSyntax Parse(ParserContext context, SourceLine header)
    {
        if (header.Content != "sample data")
        {
            context.Error(DiagnosticCodes.InvalidSampleDataSelection, $"Invalid sample data line '{header.Content}' - expected 'sample data' followed by an indented block", header.Location);
        }

        SampleDataPackReferenceSyntax? active = null;
        var mappings = new List<SampleDataMappingSyntax>();
        var bindings = new List<SampleDataBindingSyntax>();

        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            switch (LineText.FirstWord(line.Content))
            {
                case "active":
                    ParseActive(context, line, ref active);
                    break;
                case "shape":
                    ParseMapping(context, line, mappings);
                    break;
                case "bind":
                    ParseBinding(context, line, bindings);
                    break;
                default:
                    Invalid(context, line);
                    break;
            }
        }

        return new(active, mappings, bindings, header.Location);
    }

    static void ParseActive(ParserContext context, SourceLine line, ref SampleDataPackReferenceSyntax? active)
    {
        var match = ActiveRegex().Match(line.Content);
        if (!match.Success)
        {
            Invalid(context, line);
            return;
        }

        if (active is not null)
        {
            context.Error(DiagnosticCodes.DuplicateSampleDataSelection, "This sample data block already declares an active pack - it can have at most one", line.Location);
            return;
        }

        active = Reference(match.Groups[1].Value, line);
    }

    static void ParseMapping(ParserContext context, SourceLine line, List<SampleDataMappingSyntax> mappings)
    {
        var match = MappingRegex().Match(line.Content);
        if (!match.Success)
        {
            Invalid(context, line);
            return;
        }

        var mapping = new SampleDataMappingSyntax(match.Groups[1].Value, match.Groups[2].Value, line.Location);
        if (mappings.Exists(existing => existing.Shape == mapping.Shape && existing.ReadModel == mapping.ReadModel))
        {
            context.Error(DiagnosticCodes.DuplicateSampleDataSelection, $"The mapping of shape '{mapping.Shape}' to '{mapping.ReadModel}' is already declared", line.Location);
            return;
        }

        mappings.Add(mapping);
    }

    static void ParseBinding(ParserContext context, SourceLine line, List<SampleDataBindingSyntax> bindings)
    {
        var match = BindingRegex().Match(line.Content);
        if (!match.Success)
        {
            Invalid(context, line);
            return;
        }

        var binding = new SampleDataBindingSyntax(match.Groups[1].Value, Reference(match.Groups[2].Value, line), line.Location);
        if (bindings.Exists(existing => existing.ReadModel == binding.ReadModel))
        {
            context.Error(DiagnosticCodes.DuplicateSampleDataSelection, $"Read model '{binding.ReadModel}' is already bound to a pack", line.Location);
            return;
        }

        bindings.Add(binding);
    }

    static SampleDataPackReferenceSyntax Reference(string text, SourceLine line) =>
        text.StartsWith("blueprint ", StringComparison.Ordinal)
            ? new(text["blueprint ".Length..].Trim(), true, line.Location)
            : new(text, false, line.Location);

    static void Invalid(ParserContext context, SourceLine line) =>
        context.Error(
            DiagnosticCodes.InvalidSampleDataSelection,
            $"Invalid sample data line '{line.Content}' - expected 'active <pack>', 'active blueprint <id>', 'shape <Shape> maps to <ReadModel>' or 'bind <ReadModel> to <pack>'",
            line.Location);

    [GeneratedRegex(@"^active\s+((?:blueprint\s+[a-z0-9]+(?:[._-][a-z0-9]+)*)|(?:[A-Za-z_]\w*))$", RegexOptions.None, 1000)]
    private static partial Regex ActiveRegex();

    [GeneratedRegex(@"^shape\s+([A-Za-z_]\w*)\s+maps\s+to\s+([A-Za-z_]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex MappingRegex();

    [GeneratedRegex(@"^bind\s+([A-Za-z_]\w*)\s+to\s+((?:blueprint\s+[a-z0-9]+(?:[._-][a-z0-9]+)*)|(?:[A-Za-z_]\w*))$", RegexOptions.None, 1000)]
    private static partial Regex BindingRegex();
}
