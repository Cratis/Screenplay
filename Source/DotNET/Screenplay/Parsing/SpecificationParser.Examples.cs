// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Parsing;

internal static partial class SpecificationParser
{
    internal static SpecificationExampleSyntax ParseExample(ParserContext context, SourceLine header)
    {
        var match = ExampleHeaderRegex().Match(header.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidSpecificationExample, $"Invalid example declaration '{header.Content}' - expected 'example <Name> : <EventOrCommandOrReadModel>'", header.Location);
            SkipBody(context, header.Indent);
            return new(string.Empty, string.Empty, [], header.Location);
        }

        var body = ParseFixtureBody(context, header, allowGenerated: true, example: match.Groups[1].Value);

        return new(match.Groups[1].Value, match.Groups[2].Value, body.Values, header.Location)
        {
            For = body.For,
            GeneratedValues = body.Generated,
            Description = body.Description,
            DirectiveLocations = body.DirectiveLocations
        };
    }

    static FixtureBody ParseFixtureBody(ParserContext context, SourceLine parent, Match? inline = null, bool allowGenerated = false, string? example = null)
    {
        var body = new FixtureBody();
        AddInlineValue(context, parent, inline, body.Values);
        while (context.TryPeekChild(parent.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            if (example is not null && LineText.FirstWord(child.Content) == "description")
            {
                var previous = body.Description;
                body.Description = DescriptionParser.Parse(context, child, previous, $"Example '{example}'");
                if (previous is null && body.Description is not null) body.DirectiveLocations["description"] = child.Location;
                continue;
            }

            if (allowGenerated && GeneratedFixturePrefixRegex().IsMatch(child.Content))
            {
                if (ParseConcreteMapping(context, child, GeneratedFixtureRegex(), DiagnosticCodes.InvalidGeneratedFixture) is { } fixture)
                {
                    // Existing generated step fixtures retain their validator-owned PLAY0490 diagnostic.
                    if (example is null) body.Generated.Add(fixture);
                    else AddFixtureValue(context, body.Generated, fixture, body.Values);
                }
                continue;
            }

            var mapping = MappingRegex().Match(child.Content);
            if (example is not null && (LineText.FirstWord(child.Content) == "streamId" ||
                (LineText.FirstWord(child.Content) == "stream" && !mapping.Success) || child.Content.StartsWith("no stream", StringComparison.Ordinal)))
            {
                context.Error(DiagnosticCodes.InvalidSpecificationExampleBody, "An example cannot declare stream, streamId or no stream; state the route on the specification step.", child.Location);
                SkipBody(context, child.Indent);
                continue;
            }

            if (mapping.Success)
            {
                AddFixtureValue(context, body.Values, ExpressionParser.ParseMapping(context, mapping.Groups[1].Value, mapping.Groups[2], child), body.Generated);
                continue;
            }

            if (LineText.FirstWord(child.Content) == "for")
            {
                var source = child.Content["for".Length..].Trim();
                if (source.Length == 0)
                {
                    context.Error(DiagnosticCodes.InvalidSpecificationEventSource, "Invalid event-source assertion 'for' - expected 'for <value>'", child.Location);
                }
                else if (body.For is not null)
                {
                    context.Error(DiagnosticCodes.DuplicateSpecificationEventSource, "A specification step can declare its event-source assertion only once", child.Location);
                }
                else
                {
                    body.For = ExpressionParser.ParseMappingSource(context, source, child.Location);
                }
                continue;
            }

            context.Error(DiagnosticCodes.InvalidSpecificationValue, $"Invalid property mapping '{child.Content}' - expected '<property> = <value>'", child.Location);
        }

        return body;
    }

    static void AddInlineValue(ParserContext context, SourceLine line, Match? match, List<PropertyMappingSyntax> values)
    {
        if (match?.Groups["property"].Success == true)
        {
            var source = match.Groups["value"];
            var value = ParseConcrete(context, source.Value, line.LocationAt(source.Index), DiagnosticCodes.InvalidSpecificationValue);
            if (value is not null)
            {
                values.Add(new(match.Groups["property"].Value, value, line.LocationAt(match.Groups["property"].Index)));
            }
        }
    }

    static void AddFixtureValue(ParserContext context, List<PropertyMappingSyntax> values, PropertyMappingSyntax value, IEnumerable<PropertyMappingSyntax>? other = null)
    {
        if (values.Concat(other ?? []).Any(existing => existing.Property == value.Property))
        {
            context.Error(DiagnosticCodes.DuplicateSpecificationAssignment, $"Property '{value.Property}' is assigned more than once in this fixture - assign it only once, inline or indented", value.Location);
            return;
        }

        values.Add(value);
    }

    [GeneratedRegex(@"^example\s+([A-Z]\w*)\s*:\s*([A-Z]\w*(?:\.\w+)*)$", RegexOptions.None, 1000)]
    private static partial Regex ExampleHeaderRegex();

    sealed class FixtureBody
    {
        internal List<PropertyMappingSyntax> Values { get; } = [];
        internal List<PropertyMappingSyntax> Generated { get; } = [];
        internal Dictionary<string, SourceLocation> DirectiveLocations { get; } = [];
        internal ExpressionSyntax? For { get; set; }
        internal string? Description { get; set; }
    }
}
