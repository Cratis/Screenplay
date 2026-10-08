// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Reads authoring-only metadata shared by standalone and inline events.
/// </summary>
internal sealed partial class EventMetadataParser(string name)
{
    readonly Dictionary<string, SourceLocation> _locations = [];
    string? _id;
    string? _description;
    string? _documentation;

    /// <summary>
    /// Reads a metadata directive, leaving property-shaped lines to the caller.
    /// </summary>
    /// <param name="context">The parser context.</param>
    /// <param name="line">The consumed line.</param>
    /// <returns>Whether the line was a metadata directive.</returns>
    public bool TryParse(ParserContext context, SourceLine line)
    {
        switch (LineText.FirstWord(line.Content))
        {
            case "id":
                var match = IdRegex().Match(line.Content);
                if (!match.Success || string.IsNullOrWhiteSpace(StringLiteral.Unescape(match.Groups[1].Value)) || _locations.ContainsKey("id"))
                {
                    context.Error(DiagnosticCodes.InvalidEventId, $"Event '{name}' accepts one nonempty 'id \"<old-name>\"'", line.Location);
                }
                else
                {
                    _id = StringLiteral.Unescape(match.Groups[1].Value);
                    _locations["id"] = line.Location;
                    if (_id == name)
                    {
                        context.Add(new Diagnostic(DiagnosticSeverity.Information, DiagnosticCodes.RedundantEventId, $"Event '{name}' already has this name as its identity - remove the redundant id", line.Location));
                    }
                }

                return true;
            case "description":
                _description = DescriptionParser.ParseEvent(context, line, _description, $"Event '{name}'");
                _locations.TryAdd("description", line.Location);
                return true;
            case "documentation":
                _documentation = DocumentationParser.Parse(context, line, _documentation, $"Event '{name}'", _locations, DiagnosticCodes.InvalidEventDocumentation);

                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Attaches the parsed metadata to its declaration.
    /// </summary>
    /// <param name="declaration">The event declaration.</param>
    /// <returns>The event with its authoring metadata.</returns>
    public EventSyntax Apply(EventSyntax declaration) => declaration with
    {
        Id = _id,
        Description = _description,
        Documentation = _documentation,
        DirectiveLocations = declaration.DirectiveLocations.Concat(_locations).ToDictionary(entry => entry.Key, entry => entry.Value)
    };

    [GeneratedRegex(@"^id\s+""(" + StringLiteral.BodyPattern + @")""$", RegexOptions.None, 1000)]
    private static partial Regex IdRegex();
}
