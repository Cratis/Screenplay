// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses personas for the screenplay parser.
/// </summary>
internal static partial class ScreenplayParser
{
    static PersonaSyntax ParsePersona(ParserContext context, SourceLine line)
    {
        var match = PersonaRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidPersonaDeclaration, $"Invalid persona declaration '{line.Content}' - expected 'persona <Name>'", line.Location);
        }

        var name = match.Groups[1].Value;
        var body = new PersonaBodyState(name);
        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            ParsePersonaDirective(context, child, body);
        }

        return new(name, body.Description, body.Policies, line.Location)
        {
            DirectiveLocations = body.DirectiveLocations,
            Goals = body.Goals,
            Audiences = body.Audiences,
            X = body.Position?.X,
            Y = body.Position?.Y,
            Width = body.Size?.Width,
            Height = body.Size?.Height
        };
    }

    static void ParsePersonaDirective(ParserContext context, SourceLine child, PersonaBodyState body)
    {
        switch (LineText.FirstWord(child.Content))
        {
            case "goal":
                ParsePersonaGoal(context, child, body);
                break;
            case "audience":
                ParsePersonaReference(context, child, body, PersonaAudienceRegex(), body.Audiences, "audience");
                break;
            case "position":
                body.ParsePosition(context, child);
                break;
            case "size":
                body.ParseSize(context, child);
                break;
            case "description":
                body.ParseDescription(context, child, $"Persona '{body.Name}'");
                break;
            case "policy":
                ParsePersonaReference(context, child, body, PersonaPolicyRegex(), body.Policies, "policy");
                break;
            default:
                context.Error(DiagnosticCodes.UnknownPersonaDirective, $"Unexpected '{LineText.FirstWord(child.Content)}' in persona body - expected description, goal, policy, audience, position or size", child.Location);
                context.SkipBlock(child.Indent);
                break;
        }
    }

    static void ParsePersonaGoal(ParserContext context, SourceLine child, PersonaBodyState body)
    {
        if (PersonaGoalRegex().Match(child.Content) is not { Success: true } goal)
        {
            context.Error(DiagnosticCodes.InvalidPersonaDeclaration, $"Invalid goal '{child.Content}' - expected 'goal \"<text>\"'", child.Location);
            return;
        }

        body.AddValue(body.Goals, "goal", StringLiteral.Unescape(goal.Groups[1].Value), child.Location);
    }

    // An 'audience <Name>' or 'policy <Name>' line - the error code and wording follow the directive's keyword.
    static void ParsePersonaReference(ParserContext context, SourceLine child, PersonaBodyState body, Regex pattern, List<string> references, string keyword)
    {
        if (pattern.Match(child.Content) is not { Success: true } reference)
        {
            context.Error(
                keyword == "audience" ? DiagnosticCodes.InvalidAudienceDeclaration : DiagnosticCodes.InvalidPersonaPolicyReference,
                $"Invalid {keyword} reference '{child.Content}' - expected '{keyword} <Name>'",
                child.Location);
            return;
        }

        body.AddValue(references, keyword, reference.Groups[1].Value, child.Location);
    }

    sealed class PersonaBodyState(string name) : CanvasBodyState
    {
        public string Name { get; } = name;

        public List<string> Policies { get; } = [];

        public List<string> Goals { get; } = [];

        public List<string> Audiences { get; } = [];

        public void AddValue(List<string> values, string directive, string value, SourceLocation location)
        {
            values.Add(value);
            DirectiveLocations[DirectiveLocationKeys.ForValue(directive, values, values.Count - 1)] = location;
        }
    }
}
