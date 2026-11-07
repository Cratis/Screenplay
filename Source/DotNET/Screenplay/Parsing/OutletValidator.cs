// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Validates outlets over the whole document: the outlets layouts and templates declare, the ones modules, features and
/// screens change, and the items contributions add.
/// </summary>
/// <remarks>
/// A lower level's outlet, and a contribution, resolve to a declaration by name - the own module's templates first, then
/// every other module's, then the layouts. When nothing in the document declares it, the outlet is unknown, unless the
/// document uses a blueprint, whose templates may declare it. The navigation item checks mirror Scene's
/// <c>navTargetMissing</c>, <c>navSlotUnknown</c>, <c>navTargetOutOfScope</c> and <c>navModuleWithoutFeatures</c> findings.
/// </remarks>
internal sealed class OutletValidator
{
    readonly ApplicationSyntax _application;
    readonly ParserContext _context;
    readonly List<(string? Module, OutletSyntax Outlet)> _declarations = [];
    readonly Dictionary<string, HashSet<string>> _modules = new(StringComparer.Ordinal);
    readonly HashSet<string> _slots = new(StringComparer.Ordinal);
    readonly Dictionary<string, QuerySyntax> _queries = new(StringComparer.Ordinal);
    readonly HashSet<string> _screens = new(StringComparer.Ordinal);
    readonly Dictionary<string, List<OutletItemSyntax>> _contributed = new(StringComparer.Ordinal);
    readonly List<(string[] Path, SliceSyntax Slice)> _slices;
    readonly bool _usesBlueprints;

    OutletValidator(ApplicationSyntax application, ParserContext context)
    {
        _application = application;
        _context = context;
        _slices = ToolbarButtonValidator.AllSlices(application);
        foreach (var layout in application.Layouts ?? [])
        {
            _declarations.AddRange(layout.Outlets.Select(outlet => ((string?)null, outlet)));
            _slots.UnionWith(layout.Slots.Select(slot => slot.Name));
        }

        foreach (var module in application.Modules)
        {
            _declarations.AddRange(module.ScreenTemplates.SelectMany(template => template.Outlets).Select(outlet => ((string?)module.Name, outlet)));
            _declarations.AddRange((module.DialogTemplates ?? []).SelectMany(template => template.Outlets).Select(outlet => ((string?)module.Name, outlet)));
            if (!_modules.TryGetValue(module.Name, out var paths))
            {
                _modules[module.Name] = paths = new(StringComparer.Ordinal);
            }

            paths.UnionWith(module.Features.SelectMany(feature => Paths(feature, string.Empty)));
            foreach (var contribution in Contributions(module))
            {
                Contributed(contribution.ContributionPoint).AddRange(contribution.Items);
            }
        }

        foreach (var (_, slice) in _slices)
        {
            foreach (var query in slice.Queries)
            {
                _queries.TryAdd(query.Name, query);
            }

            _screens.UnionWith(slice.Screens.Select(screen => screen.Name));
        }

        _usesBlueprints = UsesBlueprints(application);
    }

    /// <summary>
    /// Validates every outlet of the application.
    /// </summary>
    /// <param name="application">The <see cref="ApplicationSyntax"/> to validate.</param>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    public static void Validate(ApplicationSyntax application, ParserContext context) => new OutletValidator(application, context).Validate();

    /// <summary>
    /// Determines whether the document uses a blueprint, whose templates may declare outlets the document does not.
    /// </summary>
    /// <param name="application">The <see cref="ApplicationSyntax"/> to read.</param>
    /// <returns><c>true</c> when a ui profile lists packages or a screen, feature or module names a blueprint template.</returns>
    public static bool UsesBlueprints(ApplicationSyntax application)
    {
        var finder = new BlueprintFinder();
        finder.VisitApplication(application);
        return finder.Found;
    }

    static IEnumerable<string> Ids(OutletItemSyntax item) => item is OutletGroupSyntax group
        ? group.Buttons.Select(button => button.Id).Prepend(group.Id)
        : [item.Id];

    static Dictionary<string, HashSet<string>> Copy(Dictionary<string, HashSet<string>> inherited) =>
        inherited.ToDictionary(entry => entry.Key, entry => new HashSet<string>(entry.Value, StringComparer.Ordinal), StringComparer.Ordinal);

    static IEnumerable<ContributionSyntax> Contributions(ModuleSyntax module) =>
        (module.Contributions ?? []).Concat(AllFeatures(module.Features).SelectMany(feature => feature.Contributions ?? []));

    static IEnumerable<FeatureSyntax> AllFeatures(IEnumerable<FeatureSyntax> features) =>
        features.SelectMany(feature => AllFeatures(feature.Features).Prepend(feature));

    static IEnumerable<string> Paths(FeatureSyntax feature, string prefix)
    {
        var path = prefix.Length == 0 ? feature.Name : $"{prefix}.{feature.Name}";
        yield return path;
        foreach (var nested in feature.Features.SelectMany(child => Paths(child, path)))
        {
            yield return nested;
        }
    }

    static OutletKind KindOf(OutletItemSyntax item) => item switch
    {
        OutletFilterSyntax => OutletKind.Filters,
        OutletTabSyntax => OutletKind.Tabs,
        OutletStatSyntax => OutletKind.Stats,
        NavigationItemSyntax => OutletKind.Navigation,
        _ => OutletKind.Actions
    };

    static string Word(OutletKind kind) => kind switch
    {
        OutletKind.Actions => "actions",
        OutletKind.Filters => "filters",
        OutletKind.Tabs => "tabs",
        OutletKind.Stats => "stats",
        _ => "navigation"
    };

    void Validate()
    {
        ValidateDeclarations();
        foreach (var module in _application.Modules)
        {
            var inherited = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            ValidateLevel(module.Outlets, module.Name, $"module '{module.Name}'", inherited);
            ValidateFeatures(module.Features, module.Name, inherited);
            foreach (var contribution in Contributions(module))
            {
                ValidateContribution(contribution, module.Name);
            }
        }
    }

    void ValidateDeclarations()
    {
        foreach (var layout in _application.Layouts ?? [])
        {
            foreach (var outlet in layout.Outlets.Where(outlet => outlet.Logo is not null).Skip(1))
            {
                _context.Error(DiagnosticCodes.NavigationLogoNotAllowed, $"The layout '{layout.Name}' declares a second logo in outlet '{outlet.Name}' - the application has one logo", outlet.Logo!.Location);
            }
        }

        foreach (var (module, outlet) in _declarations)
        {
            var owner = module is null ? $"layout outlet '{outlet.Name}'" : $"outlet '{outlet.Name}' of module '{module}'";
            if (outlet.Logo is not null && module is not null)
            {
                _context.Error(DiagnosticCodes.NavigationLogoNotAllowed, $"The {owner} declares a logo - the logo is the application's, declared once in a layout's navigation outlet", outlet.Logo.Location);
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in outlet.Items)
            {
                AddId(item, ids, [], owner);
                ValidateItem(item, outlet.Kind, owner, null);
            }
        }
    }

    void ValidateFeatures(IEnumerable<FeatureSyntax> features, string module, Dictionary<string, HashSet<string>> inherited)
    {
        foreach (var feature in features)
        {
            var own = Copy(inherited);
            ValidateLevel(feature.Outlets, module, $"feature '{feature.Name}'", own);
            foreach (var screen in feature.Slices.SelectMany(slice => slice.Screens))
            {
                ValidateLevel(screen.Outlets, module, $"screen '{screen.Name}'", Copy(own));
            }

            ValidateFeatures(feature.Features, module, own);
        }
    }

    void ValidateLevel(IEnumerable<OutletSyntax> outlets, string module, string level, Dictionary<string, HashSet<string>> inherited)
    {
        foreach (var outlet in outlets)
        {
            var owner = $"outlet '{outlet.Name}' of {level}";
            var declaration = Resolve(outlet.Name, module);
            if (declaration is null && !_usesBlueprints)
            {
                _context.Error(DiagnosticCodes.UnknownOutlet, $"The {level} changes outlet '{outlet.Name}', which no layout or template declares - declare it with 'outlet {outlet.Name} of <kind>'", outlet.Location);
            }

            if (!inherited.TryGetValue(outlet.Name, out var available))
            {
                available = new(StringComparer.Ordinal);
                if (declaration is not null)
                {
                    available.UnionWith(declaration.Items.SelectMany(Ids));
                }

                available.UnionWith(Contributed(outlet.Name).SelectMany(Ids));
                inherited[outlet.Name] = available;
            }

            var known = declaration is not null || available.Count > 0;
            foreach (var hide in outlet.Hides.Where(candidate => known && !available.Contains(candidate.Id)))
            {
                _context.Error(DiagnosticCodes.OutletItemNotInherited, $"The {owner} hides '{hide.Id}', which the outlet it inherits does not have", hide.Location);
            }

            if (outlet.Mode == OutletMode.Override && !outlet.Items.Any())
            {
                _context.Warning(DiagnosticCodes.NavOverrideWithoutItems, $"The {owner} overrides the inherited items without items of its own, so the outlet is empty there", outlet.Location);
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            var clashing = outlet.Mode == OutletMode.Extend ? available : new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in outlet.Items)
            {
                AddId(item, ids, clashing, owner);
                ValidateItem(item, declaration?.Kind, owner, module);
            }

            foreach (var item in outlet.Items.Where(candidate => candidate.Position is not null))
            {
                var position = item.Position!;
                if (known && !available.Contains(position.Id) && !ids.Contains(position.Id))
                {
                    _context.Error(DiagnosticCodes.OutletItemNotInherited, $"Item '{item.Id}' of the {owner} stands {(position.Placement == OutletPlacement.Before ? "before" : "after")} '{position.Id}', which the outlet does not have", position.Location);
                }
            }

            var after = outlet.Mode == OutletMode.Override ? new HashSet<string>(StringComparer.Ordinal) : new HashSet<string>(available, StringComparer.Ordinal);
            after.ExceptWith(outlet.Hides.Select(hide => hide.Id));
            after.UnionWith(ids);
            inherited[outlet.Name] = after;
        }
    }

    void ValidateContribution(ContributionSyntax contribution, string module)
    {
        if (!contribution.Items.Any())
        {
            return;
        }

        var declaration = Resolve(contribution.ContributionPoint, module);
        var owner = $"contribution to '{contribution.ContributionPoint}'";
        foreach (var item in contribution.Items)
        {
            ValidateItem(item, declaration?.Kind, owner, module);
        }
    }

    void ValidateItem(OutletItemSyntax item, OutletKind? kind, string owner, string? module)
    {
        var itemKind = KindOf(item);
        if (kind is { } expected && expected != itemKind)
        {
            _context.Error(DiagnosticCodes.OutletItemWrongKind, $"Item '{item.Id}' of the {owner} is a {Word(itemKind)} item, but the outlet holds {Word(expected)}", item.Location);
            return;
        }

        switch (item)
        {
            case ToolbarButtonSyntax button:
                ToolbarButtonValidator.ValidateButtonSlice(button, $"the {owner}", _slices, _context);
                break;
            case OutletGroupSyntax group:
                foreach (var button in group.Buttons)
                {
                    ToolbarButtonValidator.ValidateButtonSlice(button, $"group '{group.Id}' of the {owner}", _slices, _context);
                }

                break;
            case OutletFilterSyntax { Query: { } query } filter:
                if (!_queries.TryGetValue(query, out var filtered))
                {
                    _context.Error(DiagnosticCodes.UnknownQuery, $"Filter '{filter.Id}' of the {owner} sets a parameter of query '{query}', which the document does not declare", filter.Location);
                }
                else if (filtered.By?.Name != filter.Parameter && !filtered.Filters.Any(parameter => parameter.Name == filter.Parameter))
                {
                    _context.Error(DiagnosticCodes.OutletFilterParameterUnknown, $"Filter '{filter.Id}' of the {owner} sets '{query}.{filter.Parameter}', but query '{query}' has no parameter '{filter.Parameter}'", filter.Location);
                }

                break;
            case OutletStatSyntax { Query: { } query } stat:
                if (!_queries.TryGetValue(query, out var counted))
                {
                    _context.Error(DiagnosticCodes.UnknownQuery, $"Stat '{stat.Id}' of the {owner} reads its value from query '{query}', which the document does not declare", stat.Location);
                }
                else if (counted.ReturnType.IsCollection)
                {
                    _context.Error(DiagnosticCodes.OutletStatFromListQuery, $"Stat '{stat.Id}' of the {owner} reads its value from query '{query}', which returns a list - a stat reads a field of a single-result query", stat.Location);
                }

                break;
            case OutletTabSyntax { Shows: { } screen } tab when !_screens.Contains(screen):
                _context.Error(DiagnosticCodes.UnknownScreen, $"Tab '{tab.Id}' of the {owner} shows screen '{screen}', which the document does not declare", tab.Location);
                break;
            case NavigationItemSyntax navigation:
                ValidateNavigationItem(navigation, owner, module);
                break;
        }
    }

    void ValidateNavigationItem(NavigationItemSyntax item, string owner, string? module)
    {
        if (item.Slot is not null && _slots.Count > 0 && !_slots.Contains(item.Slot))
        {
            _context.Error(DiagnosticCodes.NavSlotUnknown, $"Navigation item '{item.Id}' in the {owner} renders into slot '{item.Slot}', which no layout declares", item.Location);
        }

        ValidateTarget(item, owner, module);
        foreach (var child in item.Children)
        {
            ValidateNavigationItem(child, owner, module);
        }
    }

    void ValidateTarget(NavigationItemSyntax item, string owner, string? module)
    {
        if (item.Target is not { } target)
        {
            if (!item.Children.Any())
            {
                _context.Error(DiagnosticCodes.NavTargetMissing, $"Navigation item '{item.Id}' in the {owner} has no target and no sub-items - add 'target <Module>[.<Feature>]'", item.Location);
            }

            return;
        }

        if (!_modules.TryGetValue(target.Module, out var features))
        {
            _context.Error(DiagnosticCodes.NavTargetMissing, $"Navigation item '{item.Id}' in the {owner} targets module '{target.Module}', which does not exist", target.Location);
            return;
        }

        if (module is not null && !string.Equals(module, target.Module, StringComparison.Ordinal))
        {
            _context.Error(DiagnosticCodes.NavTargetOutOfScope, $"Navigation item '{item.Id}' of module '{module}' targets module '{target.Module}'; a module's own items may only target that module", target.Location);
            return;
        }

        if (target.Feature is not null)
        {
            if (!features.Contains(target.Feature))
            {
                _context.Error(DiagnosticCodes.NavTargetMissing, $"Navigation item '{item.Id}' in the {owner} targets feature '{target.Module}.{target.Feature}', which does not exist", target.Location);
            }

            return;
        }

        if (features.Count == 0)
        {
            _context.Warning(DiagnosticCodes.NavModuleWithoutFeatures, $"Navigation item '{item.Id}' in the {owner} targets module '{target.Module}', which has no feature to go to", target.Location);
        }
    }

    void AddId(OutletItemSyntax item, HashSet<string> ids, HashSet<string> inherited, string owner)
    {
        foreach (var id in Ids(item))
        {
            if (!ids.Add(id))
            {
                _context.Error(DiagnosticCodes.DuplicateOutletItem, $"The {owner} already has an item with the id '{id}'", item.Location);
            }
            else if (inherited.Contains(id))
            {
                _context.Error(DiagnosticCodes.DuplicateOutletItem, $"Item '{id}' of the {owner} repeats the id of an inherited item - choose another id, or hide the inherited one", item.Location);
            }
        }
    }

    OutletSyntax? Resolve(string name, string module) =>
        _declarations.Find(declaration => declaration.Module == module && declaration.Outlet.Name == name).Outlet ??
        _declarations.Find(declaration => declaration.Module is not null && declaration.Outlet.Name == name).Outlet ??
        _declarations.Find(declaration => declaration.Outlet.Name == name).Outlet;

    List<OutletItemSyntax> Contributed(string name)
    {
        if (!_contributed.TryGetValue(name, out var items))
        {
            _contributed[name] = items = [];
        }

        return items;
    }

    sealed class BlueprintFinder : ScreenplaySyntaxWalker
    {
        public bool Found { get; private set; }

        public override void VisitNode(SyntaxNode node) => Found |= node switch
        {
            ScreenTemplateReferenceSyntax reference => reference.Blueprint is not null,
            TemplateAssignmentSyntax assignment => assignment.Blueprint is not null,
            UiProfileSyntax profile => profile.Packages.Any(),
            _ => false
        };
    }
}
