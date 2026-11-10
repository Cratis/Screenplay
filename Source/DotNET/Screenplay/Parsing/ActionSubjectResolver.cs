// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

internal sealed partial class ActionSubjectResolver(ApplicationSyntax application)
{
    readonly ConsistencyDeclarations _declarations = new(application, [.. ScreenplayValidator.ScopedSlices(application)]);
    readonly List<Diagnostic> _diagnostics = [];

    internal CompilationResult<ApplicationSyntax> Resolve()
    {
        var effective = application with
        {
            Modules = [.. application.Modules.Select(module => module with { Features = Features(module.Features) })]
        };
        return new(effective, _diagnostics);
    }

    FeatureSyntax[] Features(IEnumerable<FeatureSyntax> features) => [.. features.Select(feature => feature with
    {
        Features = Features(feature.Features),
        Slices = [.. feature.Slices.Select(Slice)]
    })];

    SliceSyntax Slice(SliceSyntax slice)
    {
        var scope = _declarations.Slices.Single(entry => ReferenceEquals(entry.Slice, slice)).Scope;
        return slice with
        {
            Screens = [.. slice.Screens.Select(screen => screen with
            {
                Directives = Container(screen.Directives, new([], []), scope),
                Contributions = [.. screen.Contributions.Select(contribution => contribution with { Directives = Container(contribution.Directives, new([], []), scope) })]
            })]
        };
    }

    ScreenDirectiveSyntax[] Container(IEnumerable<ScreenDirectiveSyntax> directives, ActionSubjectVisibility inherited, DeclarationScope scope)
    {
        var nodes = directives.ToArray();
        var local = nodes.OfType<ScreenDataSyntax>().ToArray();
        var tables = nodes.OfType<ScreenTableSyntax>().Select(table => table.Target).ToArray();
        var visible = new ActionSubjectVisibility(local.Length == 0 ? inherited.Data : local, tables.Length == 0 ? inherited.Tables : tables);
        return [.. nodes.Select(node => Directive(node, visible, scope))];
    }

    ScreenDirectiveSyntax Directive(ScreenDirectiveSyntax node, ActionSubjectVisibility visible, DeclarationScope scope) => node switch
    {
        ScreenActionSyntax action => PlainAction(action, visible, scope),
        ScreenGuardedActionSyntax action => GuardedAction(action, visible.Data, scope),
        ScreenToolbarSyntax toolbar => toolbar with { Items = [.. toolbar.Items.Select(item => ToolbarItem(item, visible, scope))] },
        ScreenSectionSyntax section => section with { Directives = Container(section.Directives, visible, scope) },
        ScreenSlotSyntax slot => slot with { Directives = Container(slot.Directives, visible, scope) },
        ScreenTemplateReferenceSyntax template => template with { Slots = [.. template.Slots.Select(slot => slot with { Directives = Container(slot.Directives, visible, scope) })] },
        ScreenComponentSyntax component => component with { Slots = [.. component.Slots.Select(slot => slot with { Directives = Container(slot.Directives, visible, scope) })] },
        _ => node
    };

    ScreenActionSyntax PlainAction(ScreenActionSyntax action, ActionSubjectVisibility visible, DeclarationScope scope)
    {
        var subjects = ItemSubjects(action.Subject, visible);
        var context = new ActionSubjectContext(subjects, scope, action.Location);
        var subject = Subject(action.Subject, action.Command, action.Inputs.Select(input => input.Name), context);
        var inputs = subject == ScreenActionSubjectKind.Item && HasSubject(subjects, action.Location, false) ? Inputs(action.Command, action.Inputs, context) : action.Inputs;
        var effective = action with { Subject = subject, Inputs = inputs };
        Remember(action, effective, subjects, scope);
        return effective;
    }

    ToolbarItemSyntax ToolbarItem(ToolbarItemSyntax item, ActionSubjectVisibility visible, DeclarationScope scope)
    {
        if (item.Kind != ToolbarItemKind.Action) return item;
        var subjects = ItemSubjects(item.Subject, visible);
        var context = new ActionSubjectContext(subjects, scope, item.Location);
        var subject = Subject(item.Subject, item.Target, item.Parameters.Select(parameter => parameter.Name), context);
        if (subject != ScreenActionSubjectKind.Item || !HasSubject(subjects, item.Location, false))
        {
            var effective = item with { Subject = subject };
            Remember(item, effective, subjects, scope);
            return effective;
        }
        var authored = item.Parameters.ToArray();
        var inputs = Inputs(item.Target, authored.Select(parameter => new InteractionArgumentSyntax(parameter.Name, string.Empty, parameter.Location)), context);
        var inferred = inputs.Skip(authored.Length).Select(input => new ScreenNavigationParameterSyntax(input.Name, new(UiBindingKind.DataContext, input.Binding, input.Location), input.Location));
        var resolved = item with { Subject = subject, Parameters = [.. authored.Concat(inferred)] };
        Remember(item, resolved, subjects, scope);
        return resolved;
    }

    bool HasSubject(IReadOnlyList<ScreenDataSyntax> subjects, Cratis.Screenplay.Diagnostics.SourceLocation location, bool guarded)
    {
        if (subjects.Count == 1) return true;
        _diagnostics.Add(new(DiagnosticSeverity.Warning, DiagnosticCodes.UnresolvedActionSubject, UnresolvedMessage(subjects, guarded), location));
        return false;
    }

    ScreenGuardedActionSyntax GuardedAction(ScreenGuardedActionSyntax action, IReadOnlyList<ScreenDataSyntax> subjects, DeclarationScope scope)
    {
        if (!HasSubject(subjects, action.Location, true)) return action;
        if (action.Subject == ScreenActionSubjectKind.Screen && subjects[0].Type.IsCollection)
        {
            _diagnostics.Add(new(DiagnosticSeverity.Warning, DiagnosticCodes.UnsupportedActionSubjectContext, "A guarded action 'for screen' cannot use a collection as 'item' - use a single-record data context or 'for item'", action.Location));
            return action;
        }

        return action with
        {
            Subject = action.Subject,
            Alternatives = [.. action.Alternatives.Select(alternative => alternative with { Arguments = Inputs(alternative.Command, alternative.Arguments, new(subjects, scope, alternative.Location)) })],
            Otherwise = action.Otherwise is { Command: { } command } otherwise
                ? otherwise with { Arguments = Inputs(command, otherwise.Arguments, new(subjects, scope, otherwise.Location)) }
                : action.Otherwise
        };
    }
}
