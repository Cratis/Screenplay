// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Parsing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Completeness;

static class CompletenessInteractions
{
    internal static IEnumerable<ScopedAction> In(ApplicationSyntax application)
    {
        var walker = new Actions(application);
        walker.VisitApplication(application);
        return walker.Collected;
    }

    internal sealed record ScopedAction(InteractionActionSyntax Action, string Target, DeclarationScope Scope);

    sealed class Actions(ApplicationSyntax application) : ScreenplaySyntaxWalker
    {
        DeclarationScope _scope = new([]);
        IReadOnlySet<string> _parameters = new HashSet<string>(StringComparer.Ordinal);
        IReadOnlyDictionary<string, string> _arguments = new Dictionary<string, string>(StringComparer.Ordinal);
        DeclarationScope? _argumentScope;

        internal List<ScopedAction> Collected { get; } = [];

        /// <inheritdoc/>
        public override void VisitModule(ModuleSyntax syntax)
        {
            var previous = _scope;
            _scope = new([.. previous.Segments, syntax.Name]);
            base.VisitModule(syntax);
            _scope = previous;
        }

        /// <inheritdoc/>
        public override void VisitFeature(FeatureSyntax syntax)
        {
            var previous = _scope;
            _scope = new([.. previous.Segments, syntax.Name]);
            base.VisitFeature(syntax);
            _scope = previous;
        }

        /// <inheritdoc/>
        public override void VisitSlice(SliceSyntax syntax)
        {
            var previous = _scope;
            _scope = new([.. previous.Segments, syntax.Name]);
            base.VisitSlice(syntax);
            _scope = previous;
        }

        /// <inheritdoc/>
        public override void VisitBehavior(BehaviorSyntax syntax)
        {
            var previous = _parameters;
            _parameters = syntax.Parameters.Select(parameter => parameter.Name).ToHashSet(StringComparer.Ordinal);
            base.VisitBehavior(syntax);
            _parameters = previous;
        }

        /// <inheritdoc/>
        public override void VisitUsesBehavior(UsesBehaviorSyntax syntax)
        {
            var matches = application.Behaviors.Where(behavior => behavior.Name == syntax.Behavior).ToArray();
            if (matches is not [var behavior])
            {
                return;
            }

            var previous = _scope;
            _argumentScope = _scope;
            _scope = new([]);
            _arguments = syntax.Arguments.ToDictionary(argument => argument.Name, argument => argument.Value, StringComparer.Ordinal);
            VisitBehavior(behavior);
            _arguments = new Dictionary<string, string>(StringComparer.Ordinal);
            _argumentScope = null;
            _scope = previous;
        }

        /// <inheritdoc/>
        public override void VisitInteractionAction(InteractionActionSyntax syntax)
        {
            var target = syntax switch
            {
                ExecuteCommandActionSyntax execute => execute.Command,
                NavigateActionSyntax navigate => navigate.Screen,
                OpenDialogActionSyntax dialog => dialog.DialogTemplate,
                _ => null
            };
            if (target is not null)
            {
                var scope = _scope;
                if (_parameters.Contains(target))
                {
                    target = _arguments.GetValueOrDefault(target);
                    scope = _argumentScope ?? scope;
                }
                if (target is not null)
                {
                    Collected.Add(new(syntax, target, scope));
                }
            }

            base.VisitInteractionAction(syntax);
        }
    }
}
