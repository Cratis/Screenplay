// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Parsing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Completeness;

static class CompletenessInteractions
{
    internal static IEnumerable<ScopedAction> In(ApplicationSyntax application, ConsistencyDeclarations declarations)
    {
        var walker = new Actions(application, declarations);
        walker.VisitApplication(application);
        return walker.Collected;
    }

    internal sealed record ScopedAction(InteractionActionSyntax Action, string Target, DeclarationScope Scope, ScreenSyntax? Screen);

    sealed class Actions(ApplicationSyntax application, ConsistencyDeclarations declarations) : ScreenplaySyntaxWalker
    {
        readonly (SyntaxNode Template, Declaration Declaration)[] _templates = [.. application.Modules.SelectMany(module =>
            module.ScreenTemplates.Cast<SyntaxNode>().Concat(module.DialogTemplates ?? []).Select(template =>
                (template, new Declaration(template is ScreenTemplateSyntax screen ? screen.Name : ((DialogTemplateSyntax)template).Name, new([module.Name])))))];
        readonly HashSet<FormSyntax> _activeForms = new(ReferenceEqualityComparer.Instance);
        DeclarationScope _scope = new([]);
        ScreenSyntax? _screen;
        BehaviorSyntax? _instantiated;
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
        public override void VisitScreenTemplate(ScreenTemplateSyntax syntax)
        {
            // Template attachments are inspected at their screen use sites.
        }

        /// <inheritdoc/>
        public override void VisitDialogTemplate(DialogTemplateSyntax syntax)
        {
            // Template attachments are inspected at their screen use sites.
        }

        /// <inheritdoc/>
        public override void VisitScreenTemplateReference(ScreenTemplateReferenceSyntax syntax)
        {
            var previous = _scope;
            if (ReferenceResolver.Resolve(syntax.Name, _scope, [.. _templates.Select(entry => entry.Declaration)]).Resolved is { } resolved)
            {
                _scope = resolved.Scope;
                var template = _templates.First(entry => entry.Declaration == resolved).Template;
                if (template is ScreenTemplateSyntax screen) base.VisitScreenTemplate(screen);
                if (template is DialogTemplateSyntax dialog) base.VisitDialogTemplate(dialog);
            }
            _scope = previous;
            base.VisitScreenTemplateReference(syntax);
        }

        /// <inheritdoc/>
        public override void VisitForm(FormSyntax syntax)
        {
            // Forms are discovered at command invocation sites, not independent UI issuers.
        }

        /// <inheritdoc/>
        public override void VisitScreenAction(ScreenActionSyntax syntax)
        {
            base.VisitScreenAction(syntax);
            DiscoverForms(syntax.Command, _scope);
        }

        /// <inheritdoc/>
        public override void VisitScreenActionAlternative(ScreenActionAlternativeSyntax syntax)
        {
            base.VisitScreenActionAlternative(syntax);
            DiscoverForms(syntax.Command, _scope);
        }

        /// <inheritdoc/>
        public override void VisitScreenActionOtherwise(ScreenActionOtherwiseSyntax syntax)
        {
            base.VisitScreenActionOtherwise(syntax);
            if (syntax.Command is not null) DiscoverForms(syntax.Command, _scope);
        }

        /// <inheritdoc/>
        public override void VisitScreen(ScreenSyntax syntax)
        {
            var previous = _screen;
            _screen = syntax;
            base.VisitScreen(syntax);
            _screen = previous;
        }

        /// <inheritdoc/>
        public override void VisitBehavior(BehaviorSyntax syntax)
        {
            if (syntax.Name is not null && !ReferenceEquals(syntax, _instantiated)) return;
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
            var previousBehavior = _instantiated;
            var previousArguments = _arguments;
            var previousArgumentScope = _argumentScope;
            _instantiated = behavior;
            _argumentScope = _scope;
            _scope = new([]);
            _arguments = syntax.Arguments.ToDictionary(argument => argument.Name, argument => argument.Value, StringComparer.Ordinal);
            VisitBehavior(behavior);
            _arguments = previousArguments;
            _argumentScope = previousArgumentScope;
            _scope = previous;
            _instantiated = previousBehavior;
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
                    Collected.Add(new(syntax, target, scope, _screen));
                    if (syntax is ExecuteCommandActionSyntax) DiscoverForms(target, scope);
                }
            }

            base.VisitInteractionAction(syntax);
        }

        void DiscoverForms(string name, DeclarationScope scope)
        {
            var command = declarations.Resolve(name, scope, slice => slice.Commands, command => command.Name)?.Node;
            if (command is null) return;
            foreach (var module in application.Modules)
            {
                var formScope = new DeclarationScope([module.Name]);
                foreach (var form in (module.Forms ?? []).Where(form => ReferenceEquals(command, declarations.Resolve(form.For, formScope, slice => slice.Commands, command => command.Name)?.Node)))
                {
                    if (!_activeForms.Add(form)) continue;
                    var previous = _scope;
                    _scope = formScope;
                    base.VisitForm(form);
                    _scope = previous;
                    _activeForms.Remove(form);
                }
            }
        }
    }
}
