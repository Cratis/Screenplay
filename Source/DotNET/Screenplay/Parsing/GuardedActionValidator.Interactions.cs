// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

internal static partial class GuardedActionValidator
{
    static ScreenDataSyntax[] TableSubjects(ScreenTableSyntax table, IReadOnlyList<ScreenDataSyntax> subjects, DeclarationScope scope, ConsistencyDeclarations declarations)
    {
        var direct = subjects.Where(subject => subject.Type.Name == table.Target).ToArray();
        if (direct.Length > 0 || subjects.Count != 1) return direct;
        var source = subjects[0];
        var fields = declarations.ViewProperties(source.Type.Name, scope)?.Where(property => property.Name == table.Target && property.Type.IsCollection).ToArray();
        return fields is { Length: 1 } ? [new(fields[0].Type, source.Query, null, table.Location)] : [];
    }

    static void ValidateInteractionUse(
        UsesBehaviorSyntax uses,
        IReadOnlyList<ScreenDataSyntax> subjects,
        DeclarationScope scope,
        ConsistencyDeclarations declarations,
        ParserContext context,
        ILookup<string, BehaviorSyntax> behaviors)
    {
        var matches = behaviors[uses.Behavior].ToArray();
        if (matches.Length == 1) ValidateInteraction(matches[0], subjects, scope, declarations, context, uses.Location);
    }

    static void ValidateInteraction(
        BehaviorSyntax behavior,
        IReadOnlyList<ScreenDataSyntax> subjects,
        DeclarationScope scope,
        ConsistencyDeclarations declarations,
        ParserContext context,
        SourceLocation? attachment = null)
    {
        foreach (var binding in behavior.Bindings.Where(binding => binding.Alternatives.Any()))
        {
            var properties = subjects.Count == 1 ? declarations.ViewProperties(subjects[0].Type.Name, scope) ?? declarations.TypeProperties(subjects[0].Type.Name) : null;
            if (subjects.Count != 1)
            {
                context.Warning(DiagnosticCodes.UnresolvedActionSubject, "A guarded interaction requires exactly one nearest 'data' subject; component context and selection do not supply 'item'", attachment ?? binding.Location);
            }

            foreach (var alternative in binding.Alternatives)
            {
                foreach (var comparison in Comparisons(alternative.Condition))
                {
                    ValidateItemPath(comparison.Left, comparison.Location, properties, scope, declarations, context);
                }

                ValidateInteractionArguments(alternative.Actions, properties, scope, declarations, context);
            }

            if (binding.Otherwise is not null) ValidateInteractionArguments(binding.Otherwise.Actions, properties, scope, declarations, context);
            var choices = binding.Alternatives.Select(alternative => new ScreenActionAlternativeSyntax(alternative.Condition, "interaction action list", alternative.Location));
            GuardedActionShadowing.Validate(new ScreenGuardedActionSyntax(string.Empty, choices, binding.Location), context);
        }
    }

    static void ValidateInteractionArguments(
        IEnumerable<InteractionActionSyntax> actions,
        IEnumerable<PropertySyntax>? properties,
        DeclarationScope scope,
        ConsistencyDeclarations declarations,
        ParserContext context)
    {
        foreach (var action in actions)
        {
            if (action is ExecuteCommandActionSyntax execute) ValidateArguments(execute.Command, execute.Arguments, properties, scope, declarations, context);
            ValidateInteractionArguments(action.OnSuccess, properties, scope, declarations, context);
            ValidateInteractionArguments(action.OnFailure, properties, scope, declarations, context);
            ValidateInteractionArguments(action.OnResult, properties, scope, declarations, context);
        }
    }

    sealed class StructuralAttachments(ILookup<string, BehaviorSyntax> behaviors, ConsistencyDeclarations declarations, ParserContext context) : ScreenplaySyntaxWalker
    {
        public override void VisitScreen(ScreenSyntax syntax)
        {
            // Screen attachments are checked with their enclosing data subject by ValidateContainer.
        }

        public override void VisitBehavior(BehaviorSyntax syntax)
        {
            if (syntax.Name is null) ValidateInteraction(syntax, [], new([]), declarations, context);
        }

        public override void VisitUsesBehavior(UsesBehaviorSyntax syntax) =>
            ValidateInteractionUse(syntax, [], new([]), declarations, context, behaviors);
    }
}
