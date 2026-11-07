// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Parsing;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Migrations;

/// <summary>
/// Moves a document from Screenplay 5 to Screenplay 6, which split what Screenplay 5 called outlets into slots - the
/// regions content is placed in - and outlets - the item lists a template offers.
/// </summary>
/// <remarks>
/// <list type="table">
/// <item><term><c>outlet &lt;name&gt;</c> (component, template, navigation)</term><description><c>slot &lt;name&gt;</c></description></item>
/// <item><term><c>destination outlet &lt;name&gt;</c></term><description><c>destination slot &lt;name&gt;</c></description></item>
/// <item><term><c>navigation topbar|sidebar</c> on a layout or template</term><description><c>outlet topbar|sidebar of navigation</c> with <c>default</c> items; buttons move to <c>outlet &lt;region&gt;Actions of actions</c></description></item>
/// <item><term><c>navigation topbar|sidebar [mode]</c> on a module</term><description><c>outlet topbar|sidebar extends</c>, or the mode it wrote; buttons move to <c>outlet &lt;region&gt;Actions</c> with the same mode</description></item>
/// <item><term><c>&lt;slot&gt; contributes &lt;Point&gt;</c></term><description><c>&lt;slot&gt;</c> and <c>outlet &lt;Point&gt; of navigation in &lt;slot&gt;</c></description></item>
/// <item><term><c>toolbar &lt;name&gt;</c> on a feature or module</term><description><c>outlet &lt;name&gt; extends</c></description></item>
/// <item><term>a template's <c>toolbar</c> slot holding items</term><description><c>outlet &lt;name&gt; of actions in &lt;slot&gt;</c> with <c>default</c> items</description></item>
/// </list>
/// What it cannot decide alone - a toolbar item that names a command rather than a slice, a slot that holds a component
/// beside its items, a logo on a module - is left where it was and reported for review.
/// </remarks>
public class SlotsAndOutlets : IScreenplayMigration
{
    readonly ScreenplayPrinter _printer = new();

    /// <inheritdoc/>
    public ScreenplayVersion From => 5;

    /// <inheritdoc/>
    public ScreenplayVersion To => 6;

    /// <inheritdoc/>
    public MigrationName Name => "slots-and-outlets";

    /// <inheritdoc/>
    public MigrationResult Apply(DocumentSyntax document)
    {
        var rewrite = new Rewrite(document, _printer);
        var application = document.Syntax;
        foreach (var form in application.RetiredForms)
        {
            rewrite.Rename(form);
        }

        var layouts = application.Layouts?.Select(layout => RewriteLayout(rewrite, layout)).ToArray();
        var offered = (layouts ?? []).SelectMany(layout => layout.Outlets)
            .Where(outlet => outlet.Kind == OutletKind.Actions)
            .Select(outlet => outlet.Name)
            .ToHashSet(StringComparer.Ordinal);
        var rewritten = application with
        {
            RetiredForms = [],
            Layouts = layouts,
            Modules = [.. application.Modules.Select(module => RewriteModule(rewrite, module, offered))]
        };

        return rewrite.Result(document with { Syntax = rewritten });
    }

    static ModuleSyntax RewriteModule(Rewrite rewrite, ModuleSyntax module, IReadOnlySet<string> offeredActions) => module with
    {
        Outlets = [.. module.Outlets, .. rewrite.Changes(module.RetiredForms, "module", offeredActions)],
        RetiredForms = [],
        ScreenTemplates = [.. module.ScreenTemplates.Select(template => RewriteScreenTemplate(rewrite, template))],
        DialogTemplates = module.DialogTemplates?.Select(template => RewriteDialogTemplate(rewrite, template)).ToArray(),
        Features = [.. module.Features.Select(feature => RewriteFeature(rewrite, feature))]
    };

    static FeatureSyntax RewriteFeature(Rewrite rewrite, FeatureSyntax feature) => feature with
    {
        Outlets = [.. feature.Outlets, .. rewrite.Changes(feature.RetiredForms, "feature", new HashSet<string>(StringComparer.Ordinal))],
        RetiredForms = [],
        Features = [.. feature.Features.Select(child => RewriteFeature(rewrite, child))]
    };

    static string Describe(ToolbarItemSyntax item) => item.Kind switch
    {
        ToolbarItemKind.Action => $"(action {item.Target})",
        ToolbarItemKind.Navigate => $"(navigate to {item.Target})",
        _ => $"(dialog {item.Target})"
    };

    static LayoutSyntax RewriteLayout(Rewrite rewrite, LayoutSyntax layout)
    {
        if (layout.RetiredForms.Count == 0)
        {
            return layout;
        }

        var (slots, outlets) = Declarations(rewrite, layout.RetiredForms, $"layout {layout.Name}", layout.Slots, layout.Outlets);
        return layout with { Slots = slots, Outlets = outlets, RetiredForms = [] };
    }

    static ScreenTemplateSyntax RewriteScreenTemplate(Rewrite rewrite, ScreenTemplateSyntax template)
    {
        if (template.RetiredForms.Count == 0)
        {
            return template;
        }

        var (slots, outlets) = Declarations(rewrite, template.RetiredForms, $"screen template {template.Name}", template.Slots, template.Outlets);
        return template with { Slots = slots, Outlets = outlets, RetiredForms = [] };
    }

    static DialogTemplateSyntax RewriteDialogTemplate(Rewrite rewrite, DialogTemplateSyntax template)
    {
        if (template.RetiredForms.Count == 0)
        {
            return template;
        }

        var (slots, outlets) = Declarations(rewrite, template.RetiredForms, $"dialog template {template.Name}", template.Slots, template.Outlets);
        return template with { Slots = slots, Outlets = outlets, RetiredForms = [] };
    }

    static (IEnumerable<SlotSyntax> Slots, IEnumerable<OutletSyntax> Outlets) Declarations(Rewrite rewrite, IReadOnlyList<RetiredForm> forms, string owner, IEnumerable<SlotSyntax> slots, IEnumerable<OutletSyntax> outlets)
    {
        var allSlots = slots.ToList();
        var allOutlets = outlets.ToList();
        foreach (var form in forms)
        {
            rewrite.Declare(form, owner, allSlots, allOutlets);
        }

        return (allSlots, allOutlets);
    }

    sealed class Rewrite(DocumentSyntax document, ScreenplayPrinter printer)
    {
        readonly IReadOnlyList<SourceLine> _lines = SourceLineSplitter.Split(document.Source, path: document.Path);
        readonly List<MigrationChange> _changes = [];
        readonly List<MigrationReview> _reviews = [];

        public MigrationResult Result(DocumentSyntax rewritten) =>
            new(rewritten, [.. _changes.OrderBy(change => change.Location.Line)], [.. _reviews.OrderBy(review => review.Location.Line)]);

        public void Rename(RetiredForm form)
        {
            if (form is not RetiredOutlet outlet)
            {
                Review(form.Location, "a retired form this step does not know - rewrite it by hand");
                return;
            }

            Change(outlet.Location, [outlet.Replacement], new ReplaceLine(outlet.Location.Line, [outlet.Replacement]));
        }

        // A module or feature changes the outlets its template offers. The layouts of the document name the actions
        // outlets they offer, so a module's region buttons can follow the layout's buttons.
        public IEnumerable<OutletSyntax> Changes(IReadOnlyList<RetiredForm> forms, string owner, IReadOnlySet<string> offeredActions)
        {
            foreach (var form in forms)
            {
                switch (form)
                {
                    case RetiredToolbar toolbar when toolbar.Toolbar.Name.Length > 0:
                        var extends = new OutletSyntax(toolbar.Toolbar.Name, null, OutletMode.Extend, toolbar.Toolbar.Buttons, toolbar.Location);
                        Change(toolbar.Location, [extends], new ReplaceLine(toolbar.Location.Line, [extends]));
                        foreach (var item in toolbar.Toolbar.Items)
                        {
                            Review(item.Location, $"toolbar item '{item.Name}' {Describe(item)} has no outlet form - write it as 'button {item.Name} title \"…\" slice <Slice>' or remove it");
                        }

                        yield return extends;
                        break;
                    case RetiredNavigationRegion region:
                        foreach (var changed in ChangeRegion(region, owner, offeredActions))
                        {
                            yield return changed;
                        }

                        break;
                    default:
                        Review(form.Location, $"a {owner} cannot hold this retired form - rewrite it by hand");
                        break;
                }
            }
        }

        // A module's region changes the layout's navigation outlet; its buttons are actions, so they change the layout's
        // actions outlet for that region - the one the layout's own region buttons moved to.
        IEnumerable<OutletSyntax> ChangeRegion(RetiredNavigationRegion region, string owner, IReadOnlySet<string> offeredActions)
        {
            var mode = region.Mode ?? OutletMode.Extend;
            var buttons = region.Items.OfType<ToolbarButtonSyntax>().ToArray();
            var outlet = new OutletSyntax(region.Region, null, mode, [.. region.Items.Where(item => item is not ToolbarButtonSyntax)], region.Location);
            var edits = new List<MigrationEdit> { new ReplaceLine(region.Location.Line, [outlet]) };
            var name = $"{region.Region}Actions";
            var actions = new OutletSyntax(name, null, mode, buttons, region.Location);
            if (buttons.Length == 0)
            {
                Change(region.Location, [outlet], [.. edits]);
            }
            else
            {
                edits.Add(new InsertAfterBlock(region.Location.Line, actions with { Items = [] }));
                edits.AddRange(buttons.Select(button => new MoveBlock(button.Location.Line, region.Location.Line, button)));
                Change(region.Location, [outlet, actions], [.. edits]);
                if (!offeredActions.Contains(name))
                {
                    Review(
                        region.Location,
                        $"moved the {region.Region} region's buttons to 'outlet {name} {Keyword(mode)}', but no layout in this document offers 'outlet {name} of actions' - check that the layout offers it, or move the buttons to the actions outlet it does offer");
                }
            }

            if (region.Logo is { } logo)
            {
                Review(logo.Location, $"a {owner} cannot declare the logo - move it to the layout's 'outlet {region.Region} of navigation'");
            }

            ReviewUnrecognized(region.Unrecognized, region.Region);
            yield return outlet;
            if (buttons.Length > 0)
            {
                yield return actions;
            }
        }

        static string Keyword(OutletMode mode) => mode switch
        {
            OutletMode.Override => "overrides",
            OutletMode.Inherit => "inherits",
            _ => "extends"
        };

        // A layout or template declares the outlets it offers, with their defaults.
        public void Declare(RetiredForm form, string owner, List<SlotSyntax> slots, List<OutletSyntax> outlets)
        {
            switch (form)
            {
                case RetiredNavigationRegion region:
                    DeclareRegion(region, owner, outlets);
                    break;
                case RetiredSlotContribution contribution:
                    var navigation = new OutletSyntax(contribution.Point, OutletKind.Navigation, OutletMode.Unspecified, [], contribution.Location) { Slot = contribution.Slot.Name };
                    Change(contribution.Location, [contribution.Slot, navigation], new ReplaceLine(contribution.Location.Line, [contribution.Slot, navigation]));
                    Add(outlets, navigation, owner);
                    break;
                case RetiredSlotItems items:
                    DeclareSlotItems(items, owner, slots, outlets);
                    break;
                default:
                    Review(form.Location, $"a {owner} cannot hold this retired form - rewrite it by hand");
                    break;
            }
        }

        void DeclareRegion(RetiredNavigationRegion region, string owner, List<OutletSyntax> outlets)
        {
            var items = region.Items.OfType<NavigationItemSyntax>().Select(item => item with { IsDefault = true }).ToArray();
            var outlet = new OutletSyntax(region.Region, OutletKind.Navigation, OutletMode.Unspecified, items, region.Location) { Logo = region.Logo };
            var edits = new List<MigrationEdit> { new ReplaceLine(region.Location.Line, [outlet]) };
            edits.AddRange(items.Select(item => new ReplaceLine(item.Location.Line, [item])));
            if (region.Mode is { } mode)
            {
                Review(region.Location, $"a {owner} declares its outlets - the mode '{mode.ToString().ToLowerInvariant()}' was dropped");
            }

            Add(outlets, outlet, owner);
            var buttons = region.Items.OfType<ToolbarButtonSyntax>().Select(button => button with { IsDefault = true }).ToArray();
            if (buttons.Length > 0)
            {
                // A navigation outlet lists destinations; the region's buttons are actions, so they get an outlet of their own.
                var actions = new OutletSyntax($"{region.Region}Actions", OutletKind.Actions, OutletMode.Unspecified, buttons, region.Location);
                edits.Add(new InsertAfterBlock(region.Location.Line, actions with { Items = [] }));
                edits.AddRange(buttons.Select(button => new MoveBlock(button.Location.Line, region.Location.Line, button)));
                Add(outlets, actions, owner);
                Change(region.Location, [outlet, actions], [.. edits]);
            }
            else
            {
                Change(region.Location, [outlet], [.. edits]);
            }

            ReviewUnrecognized(region.Unrecognized, region.Region);
        }

        void DeclareSlotItems(RetiredSlotItems items, string owner, List<SlotSyntax> slots, List<OutletSyntax> outlets)
        {
            var toolbar = items.Toolbar;
            var toolbarItems = toolbar?.Items.ToArray() ?? [];
            var buttons = (toolbar?.Buttons ?? []).Concat(items.Buttons).Select(button => button with { IsDefault = true }).ToArray();
            var outlet = new OutletSyntax(toolbar is { Name.Length: > 0 } ? toolbar.Name : items.Slot.Name, OutletKind.Actions, OutletMode.Unspecified, buttons, items.Location) { Slot = items.Slot.Name };
            var edits = new List<MigrationEdit> { new InsertAfterBlock(items.Location.Line, outlet with { Items = [] }) };
            edits.AddRange(buttons.Select(button => new MoveBlock(button.Location.Line, items.Location.Line, button)));
            if (toolbar is not null && toolbarItems.Length == 0)
            {
                edits.Add(new RemoveLine(toolbar.Location.Line));
            }

            slots.Add(items.Slot);
            Add(outlets, outlet, owner);
            Change(items.Location, [items.Slot, outlet], [.. edits]);

            foreach (var item in toolbarItems)
            {
                Review(item.Location, $"toolbar item '{item.Name}' {Describe(item)} has no outlet form - write it as 'default {item.Name} title \"…\" slice <Slice>' in outlet '{outlet.Name}' or remove it");
            }

            if (items.Others.Count > 0)
            {
                var held = buttons.Length + toolbarItems.Length;
                Review(
                    items.Location,
                    $"the {items.Slot.Name} slot on {owner} holds {held} item{(held == 1 ? string.Empty : "s")} and {items.Others.Count} other line{(items.Others.Count == 1 ? string.Empty : "s")} (a component, for example). Moved the items to outlet {outlet.Name}; left the rest where it was - place it with a screen's 'fits' or remove it");
            }
        }

        void Add(List<OutletSyntax> outlets, OutletSyntax outlet, string owner)
        {
            if (outlets.Exists(existing => existing.Name == outlet.Name))
            {
                Review(outlet.Location, $"the {owner} already declares outlet '{outlet.Name}' - merge the two");
            }

            outlets.Add(outlet);
        }

        void ReviewUnrecognized(IReadOnlyList<SourceLocation> lines, string region)
        {
            foreach (var line in lines)
            {
                Review(line, $"'{Content(line.Line)}' in the {region} region is neither an item, a button nor a logo - left where it was");
            }
        }

        void Change(SourceLocation location, IReadOnlyList<SyntaxNode> after, params MigrationEdit[] edits) =>
            _changes.Add(new(location, Content(location.Line), string.Join(" + ", after.Select(node => printer.PrintFragment(node)[0])), edits));

        void Review(SourceLocation location, string message) => _reviews.Add(new(location with { Path = location.Path ?? document.Path }, message));

        string Content(int line) => line >= 1 && line <= _lines.Count ? _lines[line - 1].Content : string.Empty;
    }
}
