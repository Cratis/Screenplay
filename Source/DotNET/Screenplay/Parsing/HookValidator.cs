// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Checks the hooks - buttons linked to State Change slices - for where and how they open their slice:
/// <c>runs</c>, <c>opens in slot</c> and <c>enabled when</c>.
/// </summary>
/// <remarks>
/// Whether the slice exists and is a State Change slice is <see cref="ToolbarButtonValidator"/>'s business; this
/// validator only looks at hooks whose slice resolves to exactly one State Change slice.
/// </remarks>
internal sealed class HookValidator
{
    readonly ParserContext _context;
    readonly List<(string[] Path, SliceSyntax Slice)> _slices;
    readonly Dictionary<string, List<(IEnumerable<SlotSyntax> Slots, OutletSyntax Outlet)>> _offers = new(StringComparer.Ordinal);
    readonly Dictionary<string, IEnumerable<SlotSyntax>> _templates = new(StringComparer.Ordinal);

    HookValidator(ApplicationSyntax application, ParserContext context)
    {
        _context = context;
        _slices = ToolbarButtonValidator.AllSlices(application);
        foreach (var layout in application.Layouts ?? [])
        {
            Offer(layout.Slots, layout.Outlets);
        }

        foreach (var module in application.Modules)
        {
            foreach (var template in module.ScreenTemplates)
            {
                _templates.TryAdd(template.Name, template.Slots);
                Offer(template.Slots, template.Outlets);
            }

            foreach (var template in module.DialogTemplates ?? [])
            {
                Offer(template.Slots, template.Outlets);
            }
        }
    }

    /// <summary>
    /// Checks every hook in an application.
    /// </summary>
    /// <param name="application">The <see cref="ApplicationSyntax"/> to check.</param>
    /// <param name="context">The <see cref="ParserContext"/> to report to.</param>
    public static void Validate(ApplicationSyntax application, ParserContext context) => new HookValidator(application, context).Validate(application);

    /// <summary>
    /// Gets whether every input of a command resolves without a form, for a hook acting on the given subject.
    /// </summary>
    /// <param name="command">The <see cref="CommandSyntax"/> the hook issues.</param>
    /// <param name="subject">What the hook acts on, or <c>null</c> when it acts on nothing in particular.</param>
    /// <returns>The first property nothing supplies, or <c>null</c> when every input resolves.</returns>
    internal static PropertySyntax? UnresolvedInput(CommandSyntax command, ScreenActionSubjectKind? subject) =>
        command.Properties.FirstOrDefault(property => !property.IsGenerated && !(property.IsIdentifier && subject is not null));

    /// <summary>
    /// Resolves the slice a hook names - by its name alone or by any trailing part of its path, up to
    /// <c>Module.Feature[.SubFeature].Slice</c>.
    /// </summary>
    /// <param name="slices">Every slice of the application with its path, from <see cref="ToolbarButtonValidator.AllSlices"/>.</param>
    /// <param name="slice">The slice the hook names.</param>
    /// <returns>The one slice it names, or <c>null</c> when it names none or more than one.</returns>
    internal static SliceSyntax? ResolveSlice(IReadOnlyList<(string[] Path, SliceSyntax Slice)> slices, string slice)
    {
        var wanted = slice.Split('.');
        var matches = slices.Where(entry => entry.Path.Length >= wanted.Length && entry.Path.TakeLast(wanted.Length).SequenceEqual(wanted, StringComparer.Ordinal)).ToList();
        return matches.Count == 1 ? matches[0].Slice : null;
    }

    static IEnumerable<ToolbarButtonSyntax> Buttons(IEnumerable<OutletItemSyntax> items) =>
        items.SelectMany<OutletItemSyntax, ToolbarButtonSyntax>(item => item switch
        {
            ToolbarButtonSyntax button => [button],
            OutletGroupSyntax group => group.Buttons,
            _ => []
        });

    static IEnumerable<ScreenDirectiveSyntax> All(IEnumerable<ScreenDirectiveSyntax> directives) =>
        directives.SelectMany(directive => All(Children(directive)).Prepend(directive));

    static IEnumerable<ScreenDirectiveSyntax> Children(ScreenDirectiveSyntax directive) => directive switch
    {
        ScreenTemplateReferenceSyntax template => template.Slots.SelectMany(slot => slot.Directives),
        ScreenSlotSyntax slot => slot.Directives,
        ScreenSectionSyntax section => section.Directives,
        ScreenComponentSyntax component => component.Slots.SelectMany(slot => slot.Directives),
        _ => []
    };

    static IEnumerable<FeatureSyntax> AllFeatures(IEnumerable<FeatureSyntax> features) =>
        features.SelectMany(feature => AllFeatures(feature.Features).Prepend(feature));

    static bool ReadsItem(ConditionSyntax condition) => condition switch
    {
        LogicalConditionSyntax logical => ReadsItem(logical.Left) || ReadsItem(logical.Right),
        ComparisonConditionSyntax comparison => comparison.Left.StartsWith("item.", StringComparison.Ordinal),
        _ => false
    };

    static ScreenFit Fit(SlotRole role) => role switch
    {
        SlotRole.Pane => ScreenFit.Pane,
        SlotRole.Panel => ScreenFit.Panel,
        _ => ScreenFit.Page
    };

    void Offer(IEnumerable<SlotSyntax> slots, IEnumerable<OutletSyntax> outlets)
    {
        foreach (var outlet in outlets.Where(outlet => outlet.Kind is not null))
        {
            if (!_offers.TryGetValue(outlet.Name, out var offers))
            {
                _offers[outlet.Name] = offers = [];
            }

            offers.Add((slots, outlet));
        }
    }

    void Validate(ApplicationSyntax application)
    {
        foreach (var offers in _offers.Values)
        {
            foreach (var (slots, outlet) in offers)
            {
                CheckOutlet(outlet, [slots], null, outlet.Subject);
            }
        }

        foreach (var module in application.Modules)
        {
            CheckChanges(module.Outlets, null);
            foreach (var feature in AllFeatures(module.Features))
            {
                CheckChanges(feature.Outlets, null);
            }

            foreach (var contribution in (module.Contributions ?? []).Concat(AllFeatures(module.Features).SelectMany(feature => feature.Contributions ?? [])))
            {
                var (slots, subject) = Offered(contribution.ContributionPoint);
                foreach (var button in Buttons(contribution.Items))
                {
                    CheckButton(button, slots, null, subject);
                }
            }
        }

        foreach (var screen in _slices.SelectMany(entry => entry.Slice.Screens))
        {
            CheckChanges(screen.Outlets, screen);
            var template = screen.Directives.OfType<ScreenTemplateReferenceSyntax>().FirstOrDefault();
            IReadOnlyList<IEnumerable<SlotSyntax>> slots = template is not null && _templates.TryGetValue(template.Name, out var declared) ? [declared] : [];
            foreach (var directive in All(screen.Directives))
            {
                switch (directive)
                {
                    case ScreenToolbarSyntax toolbar:
                        foreach (var button in toolbar.Buttons)
                        {
                            CheckButton(button, template is null ? [[]] : slots, screen, null);
                        }

                        foreach (var item in toolbar.Items)
                        {
                            CheckEnabledWhen(item.EnabledWhen, item.Subject, $"Toolbar item '{item.Name}'", item.Location);
                        }

                        break;
                }
            }
        }
    }

    (IReadOnlyList<IEnumerable<SlotSyntax>> Slots, ScreenActionSubjectKind? Subject) Offered(string outlet) =>
        _offers.TryGetValue(outlet, out var offers)
            ? ([.. offers.Select(offer => offer.Slots)], offers.Select(offer => offer.Outlet.Subject).FirstOrDefault(subject => subject is not null))
            : ([], null);

    void CheckChanges(IEnumerable<OutletSyntax> outlets, ScreenSyntax? screen)
    {
        foreach (var outlet in outlets.Where(outlet => outlet.Kind is null))
        {
            var (slots, subject) = Offered(outlet.Name);
            CheckOutlet(outlet, slots, screen, outlet.Subject ?? subject);
        }
    }

    void CheckOutlet(OutletSyntax outlet, IReadOnlyList<IEnumerable<SlotSyntax>> slots, ScreenSyntax? screen, ScreenActionSubjectKind? subject)
    {
        foreach (var button in Buttons(outlet.Items))
        {
            CheckButton(button, slots, screen, subject);
        }
    }

    void CheckButton(ToolbarButtonSyntax button, IReadOnlyList<IEnumerable<SlotSyntax>> slots, ScreenSyntax? screen, ScreenActionSubjectKind? outletSubject)
    {
        var subject = button.Subject ?? outletSubject;
        CheckEnabledWhen(button.EnabledWhen, subject, $"Toolbar button '{button.Id}'", button.Location);
        if (button.Slice is null || Resolve(button.Slice) is not { Type: SliceType.StateChange } slice)
        {
            return;
        }

        var unresolved = slice.Commands.Select(command => UnresolvedInput(command, subject)).FirstOrDefault(property => property is not null);
        if (button.Opens == HookOpening.Runs)
        {
            if (unresolved is not null)
            {
                _context.Error(
                    DiagnosticCodes.RunsHookNeedsForm,
                    $"Toolbar button '{button.Id}' runs slice '{button.Slice}' directly, but nothing supplies its input '{unresolved.Name}' without a form - remove 'runs' so the slice's screen asks for it",
                    button.Location);
            }

            return;
        }

        var screenOfSlice = slice.Screens.FirstOrDefault();
        if (screenOfSlice is null)
        {
            // A slice with no screen fits nothing and can only run: a hook that names no place runs it when it can.
            if (unresolved is null && button.Opens is null)
            {
                return;
            }

            var fix = unresolved is null
                ? "write 'runs' instead of 'opens'"
                : $"give the slice a screen - its input '{unresolved.Name}' needs a form";
            _context.Error(DiagnosticCodes.HookToScreenlessSlice, $"Toolbar button '{button.Id}' opens slice '{button.Slice}', which has no screen to open and cannot run - {fix}", button.Location);
            return;
        }

        if (button.Opens == HookOpening.Slot && button.OpensSlot is { } name)
        {
            CheckSlot(button, name, slots, screen, screenOfSlice);
        }
    }

    void CheckSlot(ToolbarButtonSyntax button, string name, IReadOnlyList<IEnumerable<SlotSyntax>> slots, ScreenSyntax? screen, ScreenSyntax target)
    {
        if (slots.Count == 0)
        {
            // The outlet comes from a blueprint or is unknown - PLAY0903 reports the latter.
            return;
        }

        var slot = slots.SelectMany(declared => declared).FirstOrDefault(declared => declared.Name == name);
        if (slot is null)
        {
            _context.Error(DiagnosticCodes.HookSlotNotDeclared, $"Toolbar button '{button.Id}' opens in slot '{name}', which the template offering it does not declare", button.Location);
            return;
        }

        if (screen?.Directives.OfType<ScreenTemplateReferenceSyntax>().Any(template => template.Slots.Any(filled => filled.Name == name)) == true)
        {
            _context.Error(DiagnosticCodes.HookSlotAlreadyFilled, $"Toolbar button '{button.Id}' opens in slot '{name}', which screen '{screen.Name}' already fills", button.Location);
            return;
        }

        if (slot.Role is { } role && !ScreenFitsParser.FitsRole(target.Fits, role))
        {
            _context.Error(
                DiagnosticCodes.HookSlotRoleNotFitted,
                $"Toolbar button '{button.Id}' opens in slot '{name}', a {ScreenFitsParser.Keyword(Fit(role))} slot, but screen '{target.Name}' fits {string.Join(", ", target.Fits.Select(ScreenFitsParser.Keyword))}",
                button.Location);
        }
    }

    void CheckEnabledWhen(ConditionSyntax? condition, ScreenActionSubjectKind? subject, string owner, SourceLocation location)
    {
        if (condition is not null && subject == ScreenActionSubjectKind.Screen && ReadsItem(condition))
        {
            _context.Error(DiagnosticCodes.EnabledWhenWithoutItem, $"{owner} is enabled when a condition on 'item' holds, but it acts 'for screen' - there is no item to read; act 'for item'", location);
        }
    }

    SliceSyntax? Resolve(string slice) => ResolveSlice(_slices, slice);
}
