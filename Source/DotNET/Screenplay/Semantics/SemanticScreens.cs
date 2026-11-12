// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Represents a screen in the stack-neutral UI contract of ESM v42: the data it shows, the commands it issues, where it
/// leads and how it is laid out. Nothing in it needs Scene to interpret.
/// </summary>
/// <param name="Id">The screen identity.</param>
/// <param name="Name">The screen name.</param>
/// <param name="Title">The title the screen shows, or its name when it declares none.</param>
/// <param name="Data">The queries the screen shows, in declaration order.</param>
/// <param name="Actions">The commands the screen issues, in declaration order.</param>
/// <param name="Navigation">The screens the screen leads to, in declaration order.</param>
/// <param name="Layout">The layout intent of the screen.</param>
public sealed record SemanticScreen(
    SemanticId Id,
    string Name,
    string Title,
    ImmutableArray<SemanticScreenData> Data,
    ImmutableArray<SemanticScreenAction> Actions,
    ImmutableArray<SemanticScreenNavigation> Navigation,
    SemanticScreenLayout Layout)
{
    /// <summary>
    /// Gets the screen description, when it has one.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the extensions the screen needs because it places their components, so a host can activate their backends ahead of use. ESM v52.
    /// </summary>
    public ImmutableArray<SemanticScreenExtension> Extensions { get; init; } = [];

    /// <summary>
    /// Gets the components the screen places through a system's extension, in declaration order. ESM v52.
    /// </summary>
    public ImmutableArray<SemanticScreenComponent> Components { get; init; } = [];
}

/// <summary>
/// Represents an extension a screen needs.
/// </summary>
/// <param name="System">The system that names the extension.</param>
/// <param name="Reference">The extension reference the system writes.</param>
public sealed record SemanticScreenExtension(SemanticId System, string Reference);

/// <summary>
/// Represents a component a screen places through a system's extension: resolved through the system, never through the package.
/// </summary>
/// <param name="Name">The name the screen gives the placement.</param>
/// <param name="System">The system that names the extension.</param>
/// <param name="Component">The component name in the extension's contract.</param>
/// <param name="Properties">The names of the properties the screen gives the component, in declaration order.</param>
/// <param name="Handles">The signals the screen handles, in declaration order, with the commands each executes.</param>
public sealed record SemanticScreenComponent(
    string Name,
    SemanticId System,
    string Component,
    ImmutableArray<string> Properties,
    ImmutableArray<SemanticScreenSignalHandler> Handles);

/// <summary>
/// Represents the handling of one signal a component raises.
/// </summary>
/// <param name="Signal">The signal name in the extension's contract.</param>
/// <param name="Commands">The commands the handler executes, in declaration order.</param>
public sealed record SemanticScreenSignalHandler(string Signal, ImmutableArray<SemanticId> Commands);

/// <summary>
/// Represents the data a screen shows through one query.
/// </summary>
/// <param name="Query">The query identity.</param>
/// <param name="Fields">The read-model fields the screen shows, in declaration order.</param>
public sealed record SemanticScreenData(SemanticId Query, ImmutableArray<SemanticScreenField> Fields)
{
    /// <summary>
    /// Gets the value the screen passes to the query, when it passes one.
    /// </summary>
    public string? By { get; init; }
}

/// <summary>
/// Represents a read-model field a screen shows.
/// </summary>
/// <param name="Property">The property path on the read model.</param>
/// <param name="Label">The label the screen shows, when it declares one.</param>
public sealed record SemanticScreenField(string Property, string? Label);

/// <summary>
/// Represents a command a screen issues.
/// </summary>
/// <param name="Command">The command identity.</param>
/// <param name="Label">The label of the action.</param>
/// <param name="Subject">What the action is issued for: <c>screen</c> or <c>item</c>.</param>
/// <param name="Inputs">The command inputs, in declaration order.</param>
public sealed record SemanticScreenAction(SemanticId Command, string Label, string Subject, ImmutableArray<SemanticScreenInput> Inputs)
{
    /// <summary>
    /// Gets the condition under which this alternative of a guarded action is issued, when it has one.
    /// </summary>
    public string? When { get; init; }

    /// <summary>
    /// Gets the screen the action leads to after it succeeds, when it leads to one.
    /// </summary>
    public SemanticId? Then { get; init; }
}

/// <summary>
/// Represents an input of a command a screen issues.
/// </summary>
/// <param name="Property">The command property identity.</param>
/// <param name="From">Where the screen takes the value from, or <see langword="null"/> when the user enters it.</param>
public sealed record SemanticScreenInput(SemanticId Property, string? From);

/// <summary>
/// Represents a screen another screen leads to.
/// </summary>
/// <param name="Screen">The target screen identity.</param>
/// <param name="Trigger">What leads there: <c>link</c>, <c>row</c>, <c>action</c>, <c>toolbar</c> or an interaction trigger.</param>
public sealed record SemanticScreenNavigation(SemanticId Screen, string Trigger)
{
    /// <summary>
    /// Gets the value passed to the target screen, when one is passed.
    /// </summary>
    public string? By { get; init; }

    /// <summary>
    /// Gets how the target opens, when it opens other than as a page.
    /// </summary>
    public string? Opens { get; init; }
}

/// <summary>
/// Represents the layout intent of a screen.
/// </summary>
/// <param name="Fits">The device classes the screen fits, in declaration order.</param>
/// <param name="Regions">The regions of the screen, in declaration order.</param>
public sealed record SemanticScreenLayout(ImmutableArray<string> Fits, ImmutableArray<SemanticScreenRegion> Regions);

/// <summary>
/// Represents a region of a screen layout.
/// </summary>
/// <param name="Kind">The region kind, such as <c>section</c>, <c>list</c> or <c>details</c>.</param>
/// <param name="Regions">The nested regions, in declaration order.</param>
public sealed record SemanticScreenRegion(string Kind, ImmutableArray<SemanticScreenRegion> Regions)
{
    /// <summary>
    /// Gets the region name, when it has one.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Gets what the region shows, when it shows a read model or a screen.
    /// </summary>
    public string? Shows { get; init; }

    /// <summary>
    /// Gets the region title, when it has one.
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// Gets the names of the commands the region issues, in declaration order.
    /// </summary>
    public ImmutableArray<string> Actions { get; init; } = [];
}
