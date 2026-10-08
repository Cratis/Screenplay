// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Validates the subject and input paths of guarded screen actions without guessing unknown shapes.
/// </summary>
internal static partial class GuardedActionValidator
{
    internal static void Validate(ApplicationSyntax application, ConsistencyDeclarations declarations, ParserContext context)
    {
        var behaviors = application.Behaviors.Where(behavior => behavior.Name is not null)
            .ToLookup(behavior => behavior.Name!, StringComparer.Ordinal);
        new StructuralAttachments(behaviors, declarations, context).VisitApplication(application);
        foreach (var (slice, scope) in declarations.Slices)
        {
            foreach (var screen in slice.Screens)
            {
                ValidateContainer(screen.Directives, [], scope, declarations, context, behaviors);
            }
        }
    }

    static void ValidateContainer(
        IEnumerable<ScreenDirectiveSyntax> directives,
        IReadOnlyList<ScreenDataSyntax> inherited,
        DeclarationScope scope,
        ConsistencyDeclarations declarations,
        ParserContext context,
        ILookup<string, BehaviorSyntax> behaviors)
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
                case ScreenBehaviorSyntax attached:
                    ValidateInteraction(attached.Behavior, subjects, scope, declarations, context);
                    break;
                case ScreenUsesBehaviorSyntax used:
                    ValidateInteractionUse(used.Uses, subjects, scope, declarations, context, behaviors);
                    break;
                case ScreenTableSyntax table:
                    var data = TableSubjects(table, subjects, scope, declarations);
                    foreach (var behavior in table.Behaviors) ValidateInteraction(behavior, data, scope, declarations, context);
                    foreach (var uses in table.UsedBehaviors) ValidateInteractionUse(uses, data, scope, declarations, context, behaviors);
                    break;
                case ScreenComponentSyntax component:
                    foreach (var behavior in component.Behaviors) ValidateInteraction(behavior, subjects, scope, declarations, context);
                    foreach (var uses in component.UsedBehaviors) ValidateInteractionUse(uses, subjects, scope, declarations, context, behaviors);
                    break;
                case ScreenSectionSyntax section:
                    ValidateContainer(section.Directives, subjects, scope, declarations, context, behaviors);
                    break;
                case ScreenSlotSyntax slot:
                    ValidateContainer(slot.Directives, subjects, scope, declarations, context, behaviors);
                    break;
                case ScreenTemplateReferenceSyntax template:
                    foreach (var slot in template.Slots) ValidateContainer(slot.Directives, subjects, scope, declarations, context, behaviors);
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
            var inputs = command?.Properties.Where(property => property.Name == argument.Name).ToArray();
            if (inputs is { Length: 0 })
            {
                context.Warning(DiagnosticCodes.UnknownActionArgumentProperty, $"Command '{commandName}' has no argument property '{argument.Name}'", argument.Location);
            }

            var subjectProperty = ValidateItemPath(argument.Binding, argument.Location, subjectProperties, scope, declarations, context, allowTerminalCollection: true);
            if (inputs is { Length: 1 } && subjectProperty is not null && inputs[0].Type.IsCollection != subjectProperty.Type.IsCollection)
            {
                context.Warning(DiagnosticCodes.UnknownActionArgumentProperty, $"Command argument '{commandName}.{argument.Name}' and subject field '{argument.Binding}' must have matching collection cardinality", argument.Location);
            }
        }
    }

    static PropertySyntax? ValidateItemPath(
        string path,
        SourceLocation location,
        IEnumerable<PropertySyntax>? properties,
        DeclarationScope scope,
        ConsistencyDeclarations declarations,
        ParserContext context,
        bool allowTerminalCollection = false)
    {
        if (!path.StartsWith("item.", StringComparison.Ordinal) || properties is null) return null;
        var segments = path["item.".Length..].Split('.');
        for (var index = 0; index < segments.Length; index++)
        {
            var matches = properties.Where(property => property.Name == segments[index]).ToArray();
            if (matches.Length == 0 || (matches.Length == 1 && matches[0].Type.IsCollection && (!allowTerminalCollection || index < segments.Length - 1)))
            {
                context.Warning(DiagnosticCodes.UnknownActionSubjectField, $"Unknown or collection-valued subject field '{path}' - guarded actions require an item field path", location);
                return null;
            }

            if (matches.Length != 1) return null;
            if (index == segments.Length - 1) return matches[0];
            var type = matches[0].Type;
            properties = declarations.TypeProperties(type.Name) ?? declarations.ViewProperties(type.Name, scope);
            if (properties is null)
            {
                declarations.Property(matches, string.Join('.', segments[index..]), out var missing);
                if (missing)
                {
                    context.Warning(DiagnosticCodes.UnknownActionSubjectField, $"Subject field '{path}' continues past scalar '{segments[index]}'", location);
                }

                return null;
            }
        }

        return null;
    }

    static IEnumerable<ComparisonConditionSyntax> Comparisons(ConditionSyntax condition) => condition switch
    {
        ComparisonConditionSyntax comparison => [comparison],
        LogicalConditionSyntax logical => Comparisons(logical.Left).Concat(Comparisons(logical.Right)),
        _ => []
    };
}
