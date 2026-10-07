// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Parsing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Completeness;

static class NavigationCompleteness
{
    internal static IEnumerable<Diagnostic> Check(ApplicationSyntax application, ConsistencyDeclarations declarations)
    {
        var graph = new NavigationGraph(application, declarations);
        graph.VisitApplication(application);
        var screens = declarations.Slices.SelectMany(entry => entry.Slice.Screens).ToArray();
        if (screens.Length > 0 && graph.Roots.Count == 0)
        {
            yield return Diagnostic.Warning(
                DiagnosticCodes.UnreachableScreen,
                $"The application has no navigation entry points from contributions or shell-level behaviors; none of its {screens.Length} screens is reachable",
                application.Location);
            yield break;
        }

        var reached = new HashSet<ScreenSyntax>(graph.Roots, ReferenceEqualityComparer.Instance);
        var pending = new Queue<ScreenSyntax>(graph.Roots);
        while (pending.TryDequeue(out var source))
        {
            foreach (var target in graph.Edges.GetValueOrDefault(source) ?? [])
            {
                if (reached.Add(target)) pending.Enqueue(target);
            }
        }

        foreach (var screen in screens.Where(screen => !reached.Contains(screen)))
        {
            yield return Diagnostic.Warning(DiagnosticCodes.UnreachableScreen, $"Screen '{screen.Name}' is unreachable from navigation entry points", screen.Location);
        }
    }

    sealed class NavigationGraph(ApplicationSyntax application, ConsistencyDeclarations declarations) : ScreenplaySyntaxWalker
    {
        readonly (FormSyntax Form, DeclarationScope Scope)[] _forms = [.. application.Modules.SelectMany(module => (module.Forms ?? []).Select(form => (form, new DeclarationScope([module.Name]))))];
        readonly (DialogTemplateSyntax Template, Declaration Declaration)[] _dialogs = [.. application.Modules.SelectMany(module => (module.DialogTemplates ?? []).Select(template => (template, new Declaration(template.Name, new([module.Name])))))];
        readonly HashSet<FormSyntax> _activeForms = new(ReferenceEqualityComparer.Instance);
        DeclarationScope _scope = new([]);
        ScreenSyntax? _screen;
        bool _visitingForm;
        BehaviorSyntax? _instantiated;
        IReadOnlyDictionary<string, string> _arguments = new Dictionary<string, string>(StringComparer.Ordinal);
        DeclarationScope? _argumentScope;

        internal HashSet<ScreenSyntax> Roots { get; } = new(ReferenceEqualityComparer.Instance);
        internal Dictionary<ScreenSyntax, HashSet<ScreenSyntax>> Edges { get; } = new(ReferenceEqualityComparer.Instance);

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
        public override void VisitScreen(ScreenSyntax syntax)
        {
            var previous = _screen;
            _screen = syntax;
            base.VisitScreen(syntax);
            _screen = previous;
        }

        /// <inheritdoc/>
        public override void VisitForm(FormSyntax syntax)
        {
            // Forms become edges only where a screen invokes their bound command, never shell roots.
            if (_visitingForm) base.VisitForm(syntax);
        }

        /// <inheritdoc/>
        public override void VisitScreenAction(ScreenActionSyntax syntax)
        {
            base.VisitScreenAction(syntax);
            var command = declarations.Resolve(syntax.Command, _scope, slice => slice.Commands, command => command.Name)?.Node;
            if (command is null) return;
            foreach (var (form, scope) in _forms.Where(entry => ReferenceEquals(command, declarations.Resolve(entry.Form.For, entry.Scope, slice => slice.Commands, command => command.Name)?.Node)))
            {
                if (!_activeForms.Add(form)) continue;
                var previous = _scope;
                _scope = scope;
                _visitingForm = true;
                VisitForm(form);
                _visitingForm = false;
                _scope = previous;
                _activeForms.Remove(form);
            }
        }

        /// <inheritdoc/>
        public override void VisitScreenNavigate(ScreenNavigateSyntax syntax) => Navigate(syntax.Screen, _scope);

        /// <inheritdoc/>
        public override void VisitBehavior(BehaviorSyntax syntax)
        {
            // A reusable declaration is not attached to anything until a 'uses' site instantiates it.
            if (syntax.Name is null || ReferenceEquals(syntax, _instantiated)) base.VisitBehavior(syntax);
        }

        /// <inheritdoc/>
        public override void VisitUsesBehavior(UsesBehaviorSyntax syntax)
        {
            if (application.Behaviors.Where(behavior => behavior.Name == syntax.Behavior).ToArray() is not [var behavior]) return;
            var previous = _scope;
            var previousBehavior = _instantiated;
            var previousArguments = _arguments;
            var previousArgumentScope = _argumentScope;
            _instantiated = behavior;
            _arguments = syntax.Arguments.ToDictionary(argument => argument.Name, argument => argument.Value, StringComparer.Ordinal);
            _argumentScope = _scope;
            _scope = new([]);
            VisitBehavior(behavior);
            _scope = previous;
            _argumentScope = previousArgumentScope;
            _arguments = previousArguments;
            _instantiated = previousBehavior;
        }

        /// <inheritdoc/>
        public override void VisitInteractionAction(InteractionActionSyntax syntax)
        {
            var target = syntax switch
            {
                NavigateActionSyntax navigate => navigate.Screen,
                OpenDialogActionSyntax dialog => dialog.DialogTemplate,
                _ => null
            };
            var scope = _scope;
            if (target is not null && _instantiated?.Parameters.Any(parameter => parameter.Name == target) == true)
            {
                target = _arguments.GetValueOrDefault(target);
                scope = _argumentScope ?? scope;
            }
            if (target is not null)
            {
                if (syntax is NavigateActionSyntax) Navigate(target, scope);
                if (syntax is OpenDialogActionSyntax) OpenDialog(target, scope);
            }
            base.VisitInteractionAction(syntax);
        }

        void Navigate(string target, DeclarationScope scope)
        {
            if (declarations.Resolve(target, scope, slice => slice.Screens, screen => screen.Name) is { } resolved) Reach(resolved.Node);
        }

        void OpenDialog(string name, DeclarationScope scope)
        {
            var template = Dialog(name, scope);
            if (template is null) return;
            foreach (var (slice, screenScope) in declarations.Slices)
            {
                foreach (var screen in slice.Screens.Where(screen => ScreenContainers.All(screen.Directives).OfType<ScreenTemplateReferenceSyntax>()
                    .Any(reference => ReferenceEquals(Dialog(reference.Name, screenScope), template))))
                {
                    Reach(screen);
                }
            }
        }

        DialogTemplateSyntax? Dialog(string name, DeclarationScope scope)
        {
            var resolution = ReferenceResolver.Resolve(name, scope, [.. _dialogs.Select(entry => entry.Declaration)]);
            return resolution.Resolved is { } resolved ? _dialogs.First(entry => entry.Declaration == resolved).Template : null;
        }

        void Reach(ScreenSyntax screen)
        {
            if (_screen is null)
            {
                Roots.Add(screen);
            }
            else
            {
                if (!Edges.TryGetValue(_screen, out var targets)) Edges[_screen] = targets = new(ReferenceEqualityComparer.Instance);
                targets.Add(screen);
            }
        }
    }
}
