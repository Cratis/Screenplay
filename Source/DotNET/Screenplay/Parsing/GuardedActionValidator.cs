// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Validates the subject and input paths of guarded screen actions without guessing unknown shapes.
/// </summary>
internal static class GuardedActionValidator
{
    internal static void Validate(ConsistencyDeclarations declarations, ParserContext context)
    {
        foreach (var (slice, scope) in declarations.Slices)
        {
            foreach (var screen in slice.Screens)
            {
                ValidateContainer(screen.Directives, [], scope, declarations, context);
            }
        }
    }

    static void ValidateContainer(
        IEnumerable<ScreenDirectiveSyntax> directives,
        IReadOnlyList<ScreenDataSyntax> inherited,
        DeclarationScope scope,
        ConsistencyDeclarations declarations,
        ParserContext context)
    {
        var local = directives.OfType<ScreenDataSyntax>().ToArray();
        var subjects = local.Length == 0 ? inherited : local;
        foreach (var directive in directives)
        {
            switch (directive)
            {
                case ScreenGuardedActionSyntax action:
                    ValidateAction(action, subjects, scope, declarations, context);
                    break;
                case ScreenSectionSyntax section:
                    ValidateContainer(section.Directives, subjects, scope, declarations, context);
                    break;
                case ScreenSlotSyntax slot:
                    ValidateContainer(slot.Directives, subjects, scope, declarations, context);
                    break;
                case ScreenTemplateReferenceSyntax template:
                    foreach (var slot in template.Slots) ValidateContainer(slot.Directives, subjects, scope, declarations, context);
                    break;
            }
        }
    }

    static void ValidateAction(
        ScreenGuardedActionSyntax action,
        IReadOnlyList<ScreenDataSyntax> subjects,
        DeclarationScope scope,
        ConsistencyDeclarations declarations,
        ParserContext context)
    {
        var properties = subjects.Count == 1 ? declarations.ViewProperties(subjects[0].Type.Name, scope) : null;
        if (subjects.Count != 1)
        {
            context.Warning(DiagnosticCodes.UnresolvedActionSubject, "A guarded action requires exactly one nearest 'data' subject; no data or equally near data bindings leave 'item' unresolved", action.Location);
        }

        foreach (var alternative in action.Alternatives)
        {
            foreach (var comparison in Comparisons(alternative.Condition))
            {
                ValidateItemPath(comparison.Left, comparison.Location, properties, scope, declarations, context);
            }

            ValidateArguments(alternative.Command, alternative.Arguments, properties, scope, declarations, context);
        }

        if (action.Otherwise is { Command: { } fallback })
        {
            ValidateArguments(fallback, action.Otherwise.Arguments, properties, scope, declarations, context);
        }

        GuardedActionShadowing.Validate(action, context);
    }

    static void ValidateArguments(
        string commandName,
        IEnumerable<InteractionArgumentSyntax> arguments,
        IEnumerable<PropertySyntax>? subjectProperties,
        DeclarationScope scope,
        ConsistencyDeclarations declarations,
        ParserContext context)
    {
        var command = declarations.Resolve(commandName, scope, slice => slice.Commands, command => command.Name)?.Node;
        foreach (var argument in arguments)
        {
            if (command?.Properties.Any(property => property.Name == argument.Name) is false)
            {
                context.Warning(DiagnosticCodes.UnknownActionArgumentProperty, $"Command '{commandName}' has no argument property '{argument.Name}'", argument.Location);
            }

            ValidateItemPath(argument.Binding, argument.Location, subjectProperties, scope, declarations, context);
        }
    }

    static void ValidateItemPath(
        string path,
        SourceLocation location,
        IEnumerable<PropertySyntax>? properties,
        DeclarationScope scope,
        ConsistencyDeclarations declarations,
        ParserContext context)
    {
        if (!path.StartsWith("item.", StringComparison.Ordinal) || properties is null) return;
        var segments = path["item.".Length..].Split('.');
        for (var index = 0; index < segments.Length; index++)
        {
            var matches = properties.Where(property => property.Name == segments[index]).ToArray();
            if (matches.Length == 0 || (matches.Length == 1 && matches[0].Type.IsCollection))
            {
                context.Warning(DiagnosticCodes.UnknownActionSubjectField, $"Unknown or collection-valued subject field '{path}' - guarded actions require an item field path", location);
                return;
            }

            if (matches.Length != 1 || index == segments.Length - 1) return;
            var type = matches[0].Type;
            properties = declarations.TypeProperties(type.Name) ?? declarations.ViewProperties(type.Name, scope);
            if (properties is null)
            {
                declarations.Property(matches, string.Join('.', segments[index..]), out var missing);
                if (missing)
                {
                    context.Warning(DiagnosticCodes.UnknownActionSubjectField, $"Subject field '{path}' continues past scalar '{segments[index]}'", location);
                }

                return;
            }
        }
    }

    static IEnumerable<ComparisonConditionSyntax> Comparisons(ConditionSyntax condition) => condition switch
    {
        ComparisonConditionSyntax comparison => [comparison],
        LogicalConditionSyntax logical => Comparisons(logical.Left).Concat(Comparisons(logical.Right)),
        _ => []
    };
}
