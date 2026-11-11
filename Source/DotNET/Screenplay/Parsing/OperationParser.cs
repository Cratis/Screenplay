// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

internal static partial class OperationParser
{
    static readonly HashSet<string> _eventMetadata = new(StringComparer.Ordinal)
    {
        "for", "tag", "generation", "origin", "documentation", "namespace", "sequence", "correlation", "causation", "causedBy", "occurred"
    };

    internal static SystemSyntax ParseSystem(ParserContext context, SourceLine header)
    {
        var match = SystemHeaderRegex().Match(header.Content);
        if (!match.Success) context.Error(DiagnosticCodes.InvalidSystemDeclaration, "Expected 'system <Name>'.", header.Location);
        string? description = null;
        var abilities = new List<AbilitySyntax>();
        var locations = new Dictionary<string, SourceLocation>();
        while (context.TryPeekChild(header.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            if (LineText.FirstWord(child.Content) == "description")
            {
                description = DescriptionParser.Parse(context, child, description, $"System '{match.Groups[1].Value}'", out _);
                locations.TryAdd("description", child.Location);
            }
            else if (LineText.FirstWord(child.Content) == "ability")
            {
                abilities.Add(ParseAbility(context, child));
            }
            else
            {
                context.Error(DiagnosticCodes.InvalidSystemDeclaration, "A system accepts a description and abilities.", child.Location);
                context.SkipBlock(child.Indent);
            }
        }

        foreach (var duplicate in abilities.GroupBy(ability => ability.Name, StringComparer.OrdinalIgnoreCase).SelectMany(group => group.Skip(1)))
            context.Error(DiagnosticCodes.DuplicateAbility, $"System '{match.Groups[1].Value}' already declares ability '{duplicate.Name}'; ability names compare ignoring case.", duplicate.Location);

        return new(match.Groups[1].Value, description, header.Location) { Abilities = abilities, DirectiveLocations = locations };
    }

    internal static AbilitySyntax ParseAbility(ParserContext context, SourceLine header)
    {
        var match = AbilityHeaderRegex().Match(header.Content);
        var name = match.Success ? match.Groups[1].Value : string.Empty;
        var effect = match.Success && match.Groups[2].Success ? match.Groups[2].Value : null;
        if (!match.Success) context.Error(DiagnosticCodes.InvalidSystemDeclaration, "Expected 'ability <Name> [safe | idempotent | once]'.", header.Location);
        else if (effect is not null && !AbilityEffectClasses.All.Contains(effect))
            context.Error(DiagnosticCodes.InvalidAbilityEffectClass, $"Ability '{name}' has effect class '{effect}'; expected safe, idempotent or once.", header.Location);

        string? description = null;
        var answersLater = false;
        var returnsSeen = false;
        var inputs = new List<PropertySyntax>();
        var returns = new List<PropertySyntax>();
        var outcomes = new List<AbilityOutcomeSyntax>();
        var inputNames = new HashSet<string>(StringComparer.Ordinal);
        var outcomeNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var locations = new Dictionary<string, SourceLocation>();
        FileReferenceSyntax? file = null;
        CodeBlockSyntax? code = null;
        ImplementationSyntax? implementation = null;
        var sourceSelected = false;
        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            var word = LineText.FirstWord(line.Content);
            if (word == "description")
            {
                description = DescriptionParser.Parse(context, line, description, $"Ability '{name}'", out _);
                locations.TryAdd("description", line.Location);
            }
            else if (line.Content == "answers later")
            {
                if (answersLater) context.Error(DiagnosticCodes.InvalidSystemDeclaration, $"Ability '{name}' repeats 'answers later'.", line.Location);
                answersLater = true;
                locations.TryAdd("answersLater", line.Location);
                RejectChildren(context, line, DiagnosticCodes.InvalidSystemDeclaration, "'answers later' cannot have children.");
            }
            else if (line.Content == "returns")
            {
                if (returnsSeen) context.Error(DiagnosticCodes.InvalidSystemDeclaration, $"Ability '{name}' repeats 'returns'.", line.Location);
                returnsSeen = true;
                var before = returns.Count;
                ParseAbilityReturns(context, line, name, returns);
                if (returns.Count == before) context.Error(DiagnosticCodes.InvalidSystemDeclaration, $"Ability '{name}' declares 'returns' without a field.", line.Location);
            }
            else if (word == "outcome")
            {
                var outcome = ParseAbilityOutcome(context, line, outcomeNames);
                if (outcome is not null) outcomes.Add(outcome);
            }
            else if (word == "implementation")
            {
                if (sourceSelected) context.Error(DiagnosticCodes.ConflictingImplementationSources, "An ability cannot mix wrapped and direct sources or repeat its wrapper.", line.Location);
                var source = ImplementationParser.ParseWrapper(context, line);
                if (!sourceSelected)
                {
                    file = source.File;
                    code = source.Code;
                    implementation = source.Implementation;
                }
                sourceSelected = true;
            }
            else if (FileReferenceParser.IsDirectiveAmongProperties(line) || line.Content.StartsWith("```", StringComparison.Ordinal))
            {
                if (sourceSelected) context.Error(DiagnosticCodes.ConflictingImplementationSources, "An ability has at most one file or inline payload.", line.Location);
                var parsedFile = FileReferenceParser.IsDirectiveAmongProperties(line) ? FileReferenceParser.Parse(context, line) : null;
                var parsedCode = parsedFile is null ? CodeBlockParser.Parse(context, line) : null;
                if (!sourceSelected)
                {
                    file = parsedFile;
                    code = parsedCode;
                }
                sourceSelected = true;
                if (parsedFile is not null) RejectChildren(context, line, DiagnosticCodes.InvalidImplementationBlock, "A file directive cannot have children.");
            }
            else if (PropertyLineParser.TryParse(line) is not null)
            {
                var property = PropertyLineParser.Parse(context, line)!;
                ValidateAbilityProperty(context, property, line, "input");
                if (!inputNames.Add(property.Name)) context.Error(DiagnosticCodes.DuplicateDeclaration, $"Ability '{name}' already declares input '{property.Name}'.", line.Location);
                inputs.Add(property);
                RejectChildren(context, line, DiagnosticCodes.InvalidSystemDeclaration, "An ability input cannot have children.");
            }
            else
            {
                context.Error(DiagnosticCodes.InvalidSystemDeclaration, "An ability accepts a description, inputs, answers later, returns, outcomes and an implementation.", line.Location);
                context.SkipBlock(line.Indent);
            }
        }

        return new(name, effect, inputs, returns, outcomes, header.Location)
        {
            Description = description,
            AnswersLater = answersLater,
            Implementation = implementation,
            File = file,
            Code = code,
            DirectiveLocations = locations
        };
    }

    internal static (OperationSyntax Operation, List<PropertyMappingSyntax> Mappings) Parse(ParserContext context, SourceLine header, bool inline = false)
    {
        var match = (inline ? InlineHeaderRegex() : HeaderRegex()).Match(header.Content);
        var name = match.Groups[1].Value;
        if (!match.Success) context.Error(DiagnosticCodes.InvalidOperationDeclaration, "Expected an operation declaration with one name.", header.Location);
        string? description = null;
        string? uses = null;
        OperationPhaseSyntax? execute = null;
        OperationPhaseSyntax? compensate = null;
        var inputs = new List<PropertySyntax>();
        var mappings = new List<PropertyMappingSyntax>();
        var names = new HashSet<string>(StringComparer.Ordinal);
        var locations = new Dictionary<string, SourceLocation>();
        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            var typed = TypedMappingRegex().Match(line.Content);
            var marker = inline && !typed.Success ? StructuredBlockParser.TrailingMarker(line.Content) : null;
            var propertyLine = line;
            if (inline && typed.Success) propertyLine = line with { Content = typed.Groups[1].Value.TrimEnd() };
            else if (marker is not null) propertyLine = line with { Content = line.Content[..^(marker.Length + 1)] };
            var structured = inline && !typed.Success && (marker is not null || context.TryPeekChild(line.Indent, out _));
            if (!typed.Success && _eventMetadata.Contains(LineText.FirstWord(line.Content)))
            {
                context.Error(DiagnosticCodes.InvalidOperationDeclaration, "Operations cannot declare event routing or metadata; escape a keyword-named input with @.", line.Location);
                context.SkipBlock(line.Indent);
                continue;
            }

            // 'uses System' is intentionally a directive; @uses escapes an input of that name.
            if (!UsesRegex().IsMatch(line.Content) && PropertyLineParser.TryParse(propertyLine) is not null)
            {
                var property = PropertyLineParser.Parse(context, propertyLine)!;
                if (inline && !typed.Success && !structured)
                {
                    context.Error(DiagnosticCodes.InvalidOperationMapping, "An inline operation input requires '<property> <Type> = <source>'.", line.Location);
                }
                if (property.IsSubject)
                {
                    context.Error(DiagnosticCodes.InvalidSubjectOwner, "The subject modifier is only valid on event properties, not operation inputs (decision 0008: one data subject per event).", line.Location);
                }
                if (property.IsIdentifier || property.IsGenerated)
                {
                    context.Error(DiagnosticCodes.InvalidOperationDeclaration, "Operation inputs cannot be identifier or generated properties.", line.Location);
                }
                if (!names.Add(property.Name)) context.Error(DiagnosticCodes.DuplicateDeclaration, $"Operation '{name}' already declares input '{property.Name}'.", line.Location);
                inputs.Add(property);
                if (inline && typed.Success) mappings.Add(ExpressionParser.ParseMapping(context, property.Name, typed.Groups[2], line));
                if (structured) mappings.Add(new(property.Name, StructuredBlockParser.ParseValue(context, line, marker), line.Location));
                RejectChildren(context, line, DiagnosticCodes.InvalidOperationDeclaration, "An operation input cannot have children.");
                continue;
            }
            if (LineText.FirstWord(line.Content) == "description")
            {
                description = DescriptionParser.Parse(context, line, description, $"Operation '{name}'", out _);
                locations.TryAdd("description", line.Location);
            }
            else if (UsesRegex().Match(line.Content) is { Success: true } reference)
            {
                if (uses is not null) context.Error(DiagnosticCodes.InvalidSystemReference, "An operation declares exactly one 'uses <System>'.", line.Location);
                else uses = reference.Groups[1].Value;
                locations.TryAdd("uses", line.LocationAt(reference.Groups[1].Index));
                RejectChildren(context, line, DiagnosticCodes.InvalidSystemReference, "A system reference cannot have children.");
            }
            else if (line.Content == "execute" || line.Content == "compensate")
            {
                var phase = ParsePhase(context, line);
                if (line.Content == "execute")
                {
                    if (execute is not null) context.Error(DiagnosticCodes.InvalidOperationDeclaration, "An operation declares execute at most once.", line.Location);
                    else execute = phase;
                }
                else if (compensate is not null)
                {
                    context.Error(DiagnosticCodes.InvalidOperationDeclaration, "An operation declares compensate at most once.", line.Location);
                }
                else
                {
                    compensate = phase;
                }
            }
            else
            {
                context.Error(DiagnosticCodes.InvalidOperationDeclaration, $"Unexpected '{line.Content}' in operation - expected uses, an input, description, execute or compensate.", line.Location);
                context.SkipBlock(line.Indent);
            }
        }
        if (uses is null) context.Error(DiagnosticCodes.InvalidSystemReference, "An operation requires exactly one 'uses <System>'.", header.Location);

        return (new(name, uses ?? string.Empty, inputs, header.Location) { Description = description, Execute = execute, Compensate = compensate, DirectiveLocations = locations }, mappings);
    }

    internal static OperationPhaseSyntax ParsePhase(ParserContext context, SourceLine header)
    {
        string? description = null;
        FileReferenceSyntax? file = null;
        CodeBlockSyntax? code = null;
        ImplementationSyntax? implementation = null;
        var sourceSelected = false;
        var locations = new Dictionary<string, SourceLocation>();
        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            if (LineText.FirstWord(line.Content) == "description")
            {
                description = DescriptionParser.Parse(context, line, description, $"Operation phase '{header.Content}'", out _);
                locations.TryAdd("description", line.Location);
                continue;
            }
            if (LineText.FirstWord(line.Content) == "implementation")
            {
                if (sourceSelected) context.Error(DiagnosticCodes.ConflictingImplementationSources, "A phase cannot mix wrapped and direct sources or repeat its wrapper.", line.Location);
                var source = ImplementationParser.ParseWrapper(context, line);
                if (!sourceSelected)
                {
                    file = source.File;
                    code = source.Code;
                    implementation = source.Implementation;
                }
                sourceSelected = true;
            }
            else if (FileReferenceParser.IsDirective(line) || line.Content.StartsWith("```", StringComparison.Ordinal))
            {
                if (sourceSelected) context.Error(DiagnosticCodes.ConflictingImplementationSources, "A phase has at most one file or inline payload.", line.Location);
                var parsedFile = FileReferenceParser.IsDirective(line) ? FileReferenceParser.Parse(context, line) : null;
                var parsedCode = parsedFile is null ? CodeBlockParser.Parse(context, line) : null;
                if (!sourceSelected)
                {
                    file = parsedFile;
                    code = parsedCode;
                }
                sourceSelected = true;
                if (parsedFile is not null) RejectChildren(context, line, DiagnosticCodes.InvalidImplementationBlock, "A file directive cannot have children.");
            }
            else
            {
                context.Error(DiagnosticCodes.InvalidOperationDeclaration, "A phase accepts description, file, a tagged fence or implementation.", line.Location);
                context.SkipBlock(line.Indent);
            }
        }

        return new(description, file, code, implementation, header.Location) { DirectiveLocations = locations };
    }

    internal static void RejectChildren(ParserContext context, SourceLine line, string code, string message)
    {
        if (!context.TryPeekChild(line.Indent, out var child)) return;
        context.Error(code, message, child.Location);
        context.SkipBlock(line.Indent);
    }

    static void ParseAbilityReturns(ParserContext context, SourceLine header, string abilityName, List<PropertySyntax> returns)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            if (PropertyLineParser.TryParse(line) is null)
            {
                context.Error(DiagnosticCodes.InvalidSystemDeclaration, "Expected '<field> <Type>' in an ability 'returns' block.", line.Location);
                context.SkipBlock(line.Indent);
                continue;
            }

            var property = PropertyLineParser.Parse(context, line)!;
            ValidateAbilityProperty(context, property, line, "returned field");
            if (!names.Add(property.Name)) context.Error(DiagnosticCodes.DuplicateDeclaration, $"Ability '{abilityName}' already returns '{property.Name}'.", line.Location);
            returns.Add(property);
            RejectChildren(context, line, DiagnosticCodes.InvalidSystemDeclaration, "A returned field cannot have children.");
        }
    }

    static AbilityOutcomeSyntax? ParseAbilityOutcome(ParserContext context, SourceLine header, HashSet<string> seen)
    {
        var match = OutcomeHeaderRegex().Match(header.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidSystemDeclaration, "Expected 'outcome <Name>'.", header.Location);
            context.SkipBlock(header.Indent);
            return null;
        }

        var name = match.Groups[1].Value;
        if (AbilityOutcomes.ReservedOutlets.Contains(name))
            context.Error(DiagnosticCodes.ReservedAbilityOutcomeName, $"Outcome '{name}' is named like a reserved outlet; reserved names are {string.Join(", ", AbilityOutcomes.ReservedOutlets.Order())}.", header.Location);
        if (!seen.Add(name)) context.Error(DiagnosticCodes.DuplicateDeclaration, $"Outcome '{name}' is declared more than once.", header.Location);
        string? description = null;
        var locations = new Dictionary<string, SourceLocation>();
        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            if (LineText.FirstWord(line.Content) == "description")
            {
                description = DescriptionParser.Parse(context, line, description, $"Outcome '{name}'", out _);
                locations.TryAdd("description", line.Location);
            }
            else
            {
                context.Error(DiagnosticCodes.InvalidSystemDeclaration, "An outcome accepts only a description and carries no payload.", line.Location);
                context.SkipBlock(line.Indent);
            }
        }

        return new(name, description, header.Location) { DirectiveLocations = locations };
    }

    static void ValidateAbilityProperty(ParserContext context, PropertySyntax property, SourceLine line, string role)
    {
        if (property.IsSubject) context.Error(DiagnosticCodes.InvalidSubjectOwner, $"The subject modifier is only valid on event properties, not ability {role}s (decision 0008: one data-subject model).", line.Location);
        if (property.IsIdentifier || property.IsGenerated) context.Error(DiagnosticCodes.InvalidSystemDeclaration, $"An ability {role} cannot be an identifier or generated property.", line.Location);
    }

    [GeneratedRegex(@"^ability\s+([A-Za-z_]\w*)(?:\s+(\S+))?$", RegexOptions.NonBacktracking)]
    private static partial Regex AbilityHeaderRegex();

    [GeneratedRegex(@"^outcome\s+([A-Za-z_]\w*)$", RegexOptions.NonBacktracking)]
    private static partial Regex OutcomeHeaderRegex();

    [GeneratedRegex(@"^system\s+([A-Za-z_]\w*)$", RegexOptions.NonBacktracking)]
    private static partial Regex SystemHeaderRegex();
    [GeneratedRegex(@"^operation\s+([A-Za-z_]\w*)$", RegexOptions.NonBacktracking)]
    private static partial Regex HeaderRegex();
    [GeneratedRegex(@"^produces\s+operation\s+([A-Za-z_]\w*)$", RegexOptions.NonBacktracking)]
    private static partial Regex InlineHeaderRegex();
    [GeneratedRegex(@"^uses\s+([A-Za-z_]\w*)$", RegexOptions.NonBacktracking)]
    private static partial Regex UsesRegex();
    [GeneratedRegex(@"^(.+?)\s*=(?:\s+(?<2>.+)|(?<2>[^=>\n].*))$", RegexOptions.NonBacktracking)]
    private static partial Regex TypedMappingRegex();
}
