// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Screenplay.Ui;

/// <summary>
/// The Scene presentation of a screen: the Scene templates, components, toolbars and outlets it uses.
/// Nothing in the neutral contract depends on it.
/// </summary>
/// <param name="Templates">The Scene templates the screen uses.</param>
/// <param name="Components">The Scene components the screen places.</param>
/// <param name="Toolbars">The toolbars the screen declares.</param>
/// <param name="Outlets">The item lists the screen offers to lower levels.</param>
public sealed record UiScene(
    IReadOnlyList<UiSceneTemplate> Templates,
    IReadOnlyList<UiSceneComponent> Components,
    IReadOnlyList<UiSceneToolbar> Toolbars,
    IReadOnlyList<UiSceneOutlet> Outlets);

/// <summary>
/// A Scene template a screen uses.
/// </summary>
/// <param name="Name">The template name.</param>
/// <param name="Slots">The slots the screen fills.</param>
public sealed record UiSceneTemplate(string Name, IReadOnlyList<string> Slots)
{
    /// <summary>
    /// Gets the blueprint the template comes from, when it names one.
    /// </summary>
    public string? Blueprint { get; init; }

    /// <summary>
    /// Gets the blueprint version, when it names one.
    /// </summary>
    public string? BlueprintVersion { get; init; }
}

/// <summary>
/// A Scene component a screen places.
/// </summary>
/// <param name="Component">The component type.</param>
/// <param name="Name">The component name on the screen.</param>
/// <param name="Properties">The component properties, each with the binding or value written for it; lists and objects keep their structure.</param>
/// <param name="Presentation">The presentation values, such as a variant or a size.</param>
public sealed record UiSceneComponent(
    string Component,
    string Name,
    IReadOnlyDictionary<string, JsonNode?> Properties,
    IReadOnlyDictionary<string, string> Presentation)
{
    /// <summary>
    /// Gets the stable identity of the component, when it has one.
    /// </summary>
    public string? StableId { get; init; }

    /// <summary>
    /// Gets the data context the component is bound to, when it is.
    /// </summary>
    public string? Context { get; init; }

    /// <summary>
    /// Gets the icon, when the component has one.
    /// </summary>
    public string? Icon { get; init; }
}

/// <summary>
/// A toolbar a screen declares.
/// </summary>
/// <param name="Name">The toolbar name.</param>
/// <param name="Buttons">The buttons, in order.</param>
public sealed record UiSceneToolbar(string Name, IReadOnlyList<UiSceneButton> Buttons);

/// <summary>
/// A button on a toolbar or in an outlet.
/// </summary>
/// <param name="Id">The button identity.</param>
/// <param name="Title">The title.</param>
public sealed record UiSceneButton(string Id, string Title)
{
    /// <summary>
    /// Gets the icon, when the button has one.
    /// </summary>
    public string? Icon { get; init; }

    /// <summary>
    /// Gets the emphasis, such as <c>primary</c>, when the button has one.
    /// </summary>
    public string? Emphasis { get; init; }

    /// <summary>
    /// Gets the confirmation question asked before the button acts, when there is one.
    /// </summary>
    public string? Confirm { get; init; }
}

/// <summary>
/// An item list a screen offers to lower levels, such as a menu or tabs.
/// </summary>
/// <param name="Name">The outlet name.</param>
/// <param name="Mode">How lower levels treat it: <c>inherit</c>, <c>extend</c> or <c>override</c>.</param>
/// <param name="Items">The items, in order.</param>
public sealed record UiSceneOutlet(string Name, string Mode, IReadOnlyList<UiSceneButton> Items)
{
    /// <summary>
    /// Gets the outlet kind, such as <c>menu</c> or <c>tabs</c>, when it names one.
    /// </summary>
    public string? Kind { get; init; }
}
