// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Ui;

/// <summary>
/// Collects the Scene presentation of one screen.
/// </summary>
sealed class UiSceneReader
{
    readonly List<UiSceneTemplate> _templates = [];
    readonly List<UiSceneComponent> _components = [];
    readonly List<UiSceneToolbar> _toolbars = [];
    readonly List<UiSceneOutlet> _outlets = [];

    /// <summary>
    /// Adds a template the screen uses.
    /// </summary>
    /// <param name="template">The template reference.</param>
    internal void Template(ScreenTemplateReferenceSyntax template) =>
        _templates.Add(new(template.Name, [.. template.Slots.Select(slot => slot.Name)]) { Blueprint = template.Blueprint, BlueprintVersion = template.Version });

    /// <summary>
    /// Adds a component the screen places.
    /// </summary>
    /// <param name="component">The component.</param>
    internal void Component(ScreenComponentSyntax component)
    {
        var properties = new SortedDictionary<string, JsonNode?>(StringComparer.Ordinal);
        foreach (var property in component.Properties)
        {
            properties[property.Property] = Value(property);
        }

        var presentation = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var value in component.Presentation) presentation[value.Name] = value.Value;
        _components.Add(new(component.Component, component.Name, properties, presentation)
        {
            StableId = component.StableId,
            Context = component.Context is { } context ? Binding(context) : null,
            Icon = component.Icon
        });
    }

    /// <summary>
    /// Adds a toolbar the screen declares, and hands every button to the screen for its action or navigation.
    /// </summary>
    /// <param name="toolbar">The toolbar.</param>
    /// <param name="button">Receives each button.</param>
    internal void Toolbar(ScreenToolbarSyntax toolbar, Action<ToolbarButtonSyntax> button)
    {
        foreach (var each in toolbar.Buttons) button(each);
        _toolbars.Add(new(toolbar.Name, [.. toolbar.Buttons.Select(Button)]));
    }

    /// <summary>
    /// Adds an outlet the screen offers, and hands every button in it to the screen for its action or navigation.
    /// </summary>
    /// <param name="outlet">The outlet.</param>
    /// <param name="button">Receives each button.</param>
    internal void Outlet(OutletSyntax outlet, Action<ToolbarButtonSyntax> button)
    {
        var items = new List<UiSceneButton>();
        foreach (var item in outlet.Items)
        {
            items.Add(item is ToolbarButtonSyntax single ? Button(single) : new(item.Id, item.Title));
            if (item is ToolbarButtonSyntax direct) button(direct);
            if (item is OutletGroupSyntax group)
            {
                foreach (var grouped in group.Buttons) button(grouped);
            }
        }

        _outlets.Add(new(outlet.Name, outlet.Mode.ToString().ToLowerInvariant(), items)
        {
            Kind = outlet.Kind?.ToString().ToLowerInvariant()
        });
    }

    /// <summary>
    /// Builds the Scene layer, or null when the screen uses no Scene vocabulary.
    /// </summary>
    /// <returns>The layer.</returns>
    internal UiScene? Build() => _templates.Count + _components.Count + _toolbars.Count + _outlets.Count == 0
        ? null
        : new(_templates, _components, _toolbars, _outlets);

    static UiSceneButton Button(ToolbarButtonSyntax button) => new(button.Id, button.Title)
    {
        Icon = button.Icon,
        Emphasis = button.Emphasis?.ToString().ToLowerInvariant(),
        Confirm = button.Confirm
    };

    static JsonNode? Value(ComponentPropertySyntax property)
    {
        if (property.Binding is { } binding) return Binding(binding);
        return property.Value is { } value ? Value(value) : null;
    }

    // Lists, objects, text and flags keep their JSON form; every other value keeps its Screenplay spelling.
    static JsonNode Value(ExpressionSyntax expression) => expression switch
    {
        ListExpressionSyntax list => new JsonArray([.. list.Items.Select(Value)]),
        ObjectExpressionSyntax record => new JsonObject(record.Members.Select(member => KeyValuePair.Create(member.Name, (JsonNode?)Value(member.Value)))),
        LiteralExpressionSyntax { Value: string text } => text,
        LiteralExpressionSyntax { Value: bool flag } => flag,
        _ => ScreenplaySyntaxText.Expression(expression)
    };

    static string Binding(UiBindingSyntax binding) => binding.BindingKind switch
    {
        UiBindingKind.QueryResult => binding.Path.Length == 0 ? $"query {binding.Query}" : $"query {binding.Query}.{binding.Path}",
        UiBindingKind.ComponentProperty => $"component {binding.Path}",
        _ => binding.Path
    };
}
