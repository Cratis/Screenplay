// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Ui;

/// <summary>
/// Reads one screen into the UI contract, walking its directives once for data, actions, navigation and regions.
/// </summary>
/// <param name="index">The model index.</param>
/// <param name="slice">The dotted path of the screen's slice.</param>
internal sealed partial class UiScreenReader(UiModelIndex index, string slice)
{
    readonly List<UiData> _data = [];
    readonly List<UiAction> _actions = [];
    readonly List<UiNavigation> _navigation = [];
    readonly Dictionary<string, List<UiField>> _fields = new(StringComparer.Ordinal);
    readonly UiSceneReader _scene = new();
    string? _title;
    string? _template;

    /// <summary>
    /// Reads a screen.
    /// </summary>
    /// <param name="screen">The screen.</param>
    /// <returns>The screen in the contract.</returns>
    internal UiScreen Read(ScreenSyntax screen)
    {
        var regions = Regions(screen.Directives.Concat(screen.Contributions.SelectMany(contribution => contribution.Directives)), true);
        _title ??= UiScreenText.FirstTitle(screen.Directives);
        foreach (var outlet in screen.Outlets) _scene.Outlet(outlet, Button);
        var data = _data.Select(entry => entry with { Fields = _fields.GetValueOrDefault(entry.ReadModel) ?? [] }).ToArray();
        var layout = new UiLayout([.. screen.Fits.Select(fit => fit.ToString().ToLowerInvariant())], regions) { Template = _template };
        return new(screen.Name, slice, _title ?? screen.Name, data, _actions, _navigation, layout)
        {
            SliceId = index.SliceId(slice),
            Description = screen.Description,
            Scene = _scene.Build()
        };
    }

    List<UiRegion> Regions(IEnumerable<ScreenDirectiveSyntax> directives, bool top)
    {
        var regions = new List<UiRegion>();
        foreach (var directive in directives)
        {
            if (Region(directive, top) is { } region) regions.Add(region);
        }

        return regions;
    }

    UiRegion? Region(ScreenDirectiveSyntax directive, bool top)
    {
        switch (directive)
        {
            case ScreenTitleSyntax title:
                if (top) _title ??= title.Text;
                return null;
            case ScreenDataSyntax data:
                Data(data);
                return null;
            case ScreenActionSyntax action:
                Action(action);
                return null;
            case ScreenGuardedActionSyntax guarded:
                Guarded(guarded);
                return null;
            case ScreenNavigateSyntax navigate:
                _navigation.Add(UiScreenText.Navigation(navigate, "link"));
                return null;
            case ScreenTemplateReferenceSyntax template:
                if (top) _template ??= template.Name;
                _scene.Template(template);
                return new("template", [.. template.Slots.Select(slot => Container("slot", slot.Name, slot.Directives))]) { Name = template.Name };
            case ScreenSlotSyntax slot:
                return Container("slot", slot.Name, slot.Directives);
            case ScreenSectionSyntax section:
                return Container("section", section.Name, section.Directives);
            case ScreenTableSyntax table:
                Fields(table.Target, table.Columns.Select(column => new UiField(column.Property, column.Label)));
                if (table.RowClick is { } row) _navigation.Add(UiScreenText.Navigation(row, "row"));
                Behaviors(table.Behaviors, ScreenActionSubjectKind.Item);
                return new("list", []) { Shows = table.Target };
            case ScreenBehaviorSyntax behavior:
                Behaviors([behavior.Behavior], null);
                return null;
            case ScreenSummarySyntax summary:
                Fields(summary.Target, summary.Fields.Select(field => new UiField(field.Property, field.Label)));
                return new("details", []) { Shows = summary.Target };
            case ScreenShowSyntax show:
                return new("embed", []) { Shows = show.Screen, Name = show.Element };
            case ScreenComponentSyntax component:
                _scene.Component(component);
                Behaviors(component.Behaviors, null);
                return new("component", [.. component.Slots.Select(slot => Container("slot", slot.Name, slot.Directives))]) { Name = component.Name };
            case ScreenToolbarSyntax toolbar:
                Toolbar(toolbar);
                return new("toolbar", []) { Name = toolbar.Name };
            default:
                return null;
        }
    }

    UiRegion Container(string kind, string name, IEnumerable<ScreenDirectiveSyntax> directives)
    {
        var before = _actions.Count;
        var regions = Regions(directives, false);
        return new(kind, regions)
        {
            Name = name,
            Title = directives.OfType<ScreenTitleSyntax>().FirstOrDefault()?.Text,
            Actions = [.. _actions.Skip(before).Select(action => action.Command).Distinct(StringComparer.Ordinal)]
        };
    }

    void Data(ScreenDataSyntax data)
    {
        var found = index.Query(slice, data.Query);
        var operation = found is { } query ? index.Operation(query.Slice, query.Query.Name) : null;
        var readModel = found?.Query.ReturnType.Name ?? data.Type.Name;
        _data.Add(new(data.Query, readModel, found?.Query.ReturnType.IsCollection ?? data.Type.IsCollection, [])
        {
            By = data.By,
            QueryId = operation?.Id.ToString(),
            Route = operation?.Route,
            Live = operation?.Live ?? false
        });
    }

    void Fields(string target, IEnumerable<UiField> fields)
    {
        if (!_fields.TryGetValue(target, out var known)) _fields[target] = known = [];
        known.AddRange(fields.Where(field => !known.Exists(existing => existing.Property == field.Property)));
    }
}
