// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses concepts for the screenplay parser.
/// </summary>
internal static partial class ScreenplayParser
{
    static ConceptSyntax ParseConcept(ParserContext context, SourceLine line)
    {
        var match = ConceptRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidConceptDeclaration, $"Invalid concept declaration '{line.Content}' - expected 'concept <Name> : <Type>'", line.Location);
            context.SkipBlock(line.Indent);
            return new(LineText.FirstWord(line.Content), string.Empty, [], [], line.Location);
        }

        var name = match.Groups[1].Value;
        var type = match.Groups[2].Value;
        var attributes = ConceptComplianceParser.ParseMarkers(context, line, name, match.Groups[3].Value);

        if (type != "Enum" && !ConceptSyntax.PrimitiveTypes.Contains(type))
        {
            context.Error(DiagnosticCodes.UnknownPrimitiveType, $"Unknown primitive type '{type}' - expected {string.Join(", ", ConceptSyntax.PrimitiveTypes)} or Enum", line.Location);
        }

        var body = new ConceptBodyState(name, type, attributes);
        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            ParseConceptDirective(context, child, body);
        }

        return new(name, type, attributes, body.Values, line.Location, body.Validations)
        {
            Generation = body.Generation,
            Description = body.Description,
            File = body.File,
            ValueNumbers = body.Members.Finish(context, line, body.Values.Count),
            DirectiveLocations = body.DirectiveLocations
        };
    }

    static void ParseConceptDirective(ParserContext context, SourceLine child, ConceptBodyState body)
    {
        if (TryParseConceptMember(context, child, body) || TryParseConceptRule(context, child, body))
        {
            return;
        }

        if (!body.IsEnumeration)
        {
            context.Error(DiagnosticCodes.UnknownConceptDirective, $"Unexpected '{child.Content}' in concept body - expected validate, generate, 'file <path>' or '<attribute> reason \"<text>\"'{MisplacedRuleHint.ForConcept(child.Content)}", child.Location);
            context.SkipBlock(child.Indent);
        }
        else if (EnumValueRegex().IsMatch(child.Content))
        {
            body.AddValue(LineText.Unescape(child.Content), child.Location);
        }
        else
        {
            context.Error(DiagnosticCodes.InvalidEnumerationValue, $"Invalid enum value '{child.Content}' - expected an identifier", child.Location);
        }
    }

    static bool TryParseConceptMember(ParserContext context, SourceLine child, ConceptBodyState body) =>
        TryParseNumberedMember(context, child, body) || TryParseConceptDescription(context, child, body);

    static bool TryParseNumberedMember(ParserContext context, SourceLine child, ConceptBodyState body)
    {
        if (!body.IsEnumeration || !body.Members.TryNumbered(context, child, out var memberName))
        {
            return false;
        }

        body.AddValue(memberName, child.Location);
        return true;
    }

    static bool TryParseConceptDescription(ParserContext context, SourceLine child, ConceptBodyState body)
    {
        var description = body.Description;
        var claimed = DescriptionParser.TryParse(context, child, ref description, $"Concept '{body.Name}'", body.DirectiveLocations, body.IsEnumeration);
        body.Description = description;
        return claimed;
    }

    // The directives of a concept body that are keyed by their first word, or by what a line points at.
    static bool TryParseConceptRule(ParserContext context, SourceLine child, ConceptBodyState body)
    {
        if (FileReferenceParser.IsDirective(child))
        {
            body.File = FileReferenceParser.ParseReplacing(context, child, body.File, body.DirectiveLocations);
            return true;
        }

        return TryParseConceptValidation(context, child, body)
            || TryParseConceptGeneration(context, child, body)
            || ConceptComplianceParser.TryParseDirective(context, child, body.Name, body.Attributes, body.DirectiveLocations);
    }

    static bool TryParseConceptValidation(ParserContext context, SourceLine child, ConceptBodyState body)
    {
        if (LineText.FirstWord(child.Content) != "validate")
        {
            return false;
        }

        if (body.IsBareEnumerationKeyword(child, "validate") && !context.TryPeekChild(child.Indent, out _))
        {
            context.Warning(
                DiagnosticCodes.ValidateReadAsEnumerationBlock,
                $"'validate' in enumeration concept '{body.Name}' declares an empty validate block, not a value named 'validate' - write '@validate' for the value",
                child.Location);
        }

        if (ValidateParser.Parse(context, child, ValidationOwnerKind.Concept) is { } validate)
        {
            body.Validations.Add(validate);
        }

        return true;
    }

    static bool TryParseConceptGeneration(ParserContext context, SourceLine child, ConceptBodyState body)
    {
        if (LineText.FirstWord(child.Content) != "generate" || body.IsBareEnumerationKeyword(child, "generate"))
        {
            return false;
        }

        if (body.DeclaredGeneration)
        {
            context.Error(DiagnosticCodes.DuplicateGeneration, $"Concept '{body.Name}' declares generation more than once.", child.Location);
        }

        var parsed = GenerationParser.Parse(context, child, body.Type);
        if (!body.DeclaredGeneration)
        {
            body.Generation = parsed;
            body.DirectiveLocations["generate"] = child.Location;
        }

        body.DeclaredGeneration = true;
        return true;
    }

    sealed class ConceptBodyState(string name, string type, List<ConceptAttributeSyntax> attributes)
    {
        public string Name { get; } = name;

        public string Type { get; } = type;

        public List<ConceptAttributeSyntax> Attributes { get; } = attributes;

        public bool IsEnumeration { get; } = type == "Enum";

        public List<string> Values { get; } = [];

        public EnumerationMembers Members { get; } = new();

        public GenerationSyntax? Generation { get; set; }

        public bool DeclaredGeneration { get; set; }

        public string? Description { get; set; }

        public List<ValidateSyntax> Validations { get; } = [];

        public Dictionary<string, SourceLocation> DirectiveLocations { get; } = [];

        public FileReferenceSyntax? File { get; set; }

        public bool IsBareEnumerationKeyword(SourceLine child, string keyword) => IsEnumeration && child.Content == keyword;

        public void AddValue(string value, SourceLocation location)
        {
            Values.Add(value);
            DirectiveLocations[DirectiveLocationKeys.ForValue("value", Values, Values.Count - 1)] = location;
        }
    }
}
