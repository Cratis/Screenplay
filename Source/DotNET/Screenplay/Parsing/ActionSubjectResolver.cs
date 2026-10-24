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
                Directives = Container(screen.Directives, [], scope),
                Contributions = [.. screen.Contributions.Select(contribution => contribution with { Directives = Container(contribution.Directives, [], scope) })]
            })]
        };
    }

    ScreenDirectiveSyntax[] Container(IEnumerable<ScreenDirectiveSyntax> directives, IReadOnlyList<ScreenDataSyntax> inherited, DeclarationScope scope)
    {
        var nodes = directives.ToArray();
        var local = nodes.OfType<ScreenDataSyntax>().ToArray();
        var subjects = local.Length == 0 ? inherited : local;
        return [.. nodes.Select(node => Directive(node, subjects, scope))];
    }

    ScreenDirectiveSyntax Directive(ScreenDirectiveSyntax node, IReadOnlyList<ScreenDataSyntax> subjects, DeclarationScope scope) => node switch
    {
        ScreenActionSyntax action => PlainAction(action, subjects, scope),
        ScreenGuardedActionSyntax action => GuardedAction(action, subjects, scope),
        ScreenToolbarSyntax toolbar => toolbar with { Items = [.. toolbar.Items.Select(item => ToolbarItem(item, subjects, scope))] },
        ScreenSectionSyntax section => section with { Directives = Container(section.Directives, subjects, scope) },
        ScreenSlotSyntax slot => slot with { Directives = Container(slot.Directives, subjects, scope) },
        ScreenTemplateReferenceSyntax template => template with { Slots = [.. template.Slots.Select(slot => slot with { Directives = Container(slot.Directives, subjects, scope) })] },
        ScreenComponentSyntax component => component with { Outlets = [.. component.Outlets.Select(outlet => outlet with { Directives = Container(outlet.Directives, subjects, scope) })] },
        _ => node
    };

    ScreenActionSyntax PlainAction(ScreenActionSyntax action, IReadOnlyList<ScreenDataSyntax> subjects, DeclarationScope scope)
    {
        var subject = Subject(action.Subject, action.Command, subjects, scope, action.Location);
        var inputs = subject == ScreenActionSubjectKind.Item ? Inputs(action.Command, action.Inputs, subjects, scope, action.Location) : action.Inputs;
        var effective = action with { Subject = subject, Inputs = inputs };
        Remember(action, effective, subjects, scope);
        return effective;
    }

    ToolbarItemSyntax ToolbarItem(ToolbarItemSyntax item, IReadOnlyList<ScreenDataSyntax> subjects, DeclarationScope scope)
    {
        if (item.Kind != ToolbarItemKind.Action) return item;
        var subject = Subject(item.Subject, item.Target, subjects, scope, item.Location);
        if (subject != ScreenActionSubjectKind.Item)
        {
            var effective = item with { Subject = subject };
            Remember(item, effective, subjects, scope);
            return effective;
        }
        var authored = item.Parameters.ToArray();
        var inputs = Inputs(item.Target, authored.Select(parameter => new InteractionArgumentSyntax(parameter.Name, string.Empty, parameter.Location)), subjects, scope, item.Location);
        var inferred = inputs.Skip(authored.Length).Select(input => new ScreenNavigationParameterSyntax(input.Name, new(UiBindingKind.DataContext, input.Binding, input.Location), input.Location));
        var resolved = item with { Subject = subject, Parameters = [.. authored.Concat(inferred)] };
        Remember(item, resolved, subjects, scope);
        return resolved;
    }

    ScreenGuardedActionSyntax GuardedAction(ScreenGuardedActionSyntax action, IReadOnlyList<ScreenDataSyntax> subjects, DeclarationScope scope)
    {
        if (action.Subject == ScreenActionSubjectKind.Screen && subjects.Count == 1 && subjects[0].Type.IsCollection)
        {
            _diagnostics.Add(new(DiagnosticSeverity.Warning, DiagnosticCodes.UnsupportedActionSubjectContext, "A guarded action 'for screen' cannot use a collection as 'item' - use a single-record data context or 'for item'", action.Location));
            return action;
        }

        return action with
        {
            Subject = action.Subject ?? ScreenActionSubjectKind.Item,
            Alternatives = [.. action.Alternatives.Select(alternative => alternative with { Arguments = Inputs(alternative.Command, alternative.Arguments, subjects, scope, alternative.Location) })],
            Otherwise = action.Otherwise is { Command: { } command } otherwise
                ? otherwise with { Arguments = Inputs(command, otherwise.Arguments, subjects, scope, otherwise.Location) }
                : action.Otherwise
        };
    }
}
