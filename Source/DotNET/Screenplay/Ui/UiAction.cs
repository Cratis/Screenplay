// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Ui;

/// <summary>
/// A command a screen issues.
/// </summary>
/// <param name="Command">The command name.</param>
/// <param name="Label">The label a user sees; the command name when the model gives none.</param>
/// <param name="Subject">What the action applies to: <c>screen</c> for the screen as a whole, or <c>item</c> for one shown item.</param>
/// <param name="Inputs">The command's inputs, in declaration order.</param>
public sealed record UiAction(string Command, string Label, string Subject, IReadOnlyList<UiInput> Inputs)
{
    /// <summary>
    /// Gets the condition under which this command is the one issued, for an action that picks one of several commands.
    /// </summary>
    public string? When { get; init; }

    /// <summary>
    /// Gets the screen shown after the command succeeds, when there is one.
    /// </summary>
    public string? Then { get; init; }

    /// <summary>
    /// Gets the semantic identity of the command, when the model has an executable model.
    /// </summary>
    public string? CommandId { get; init; }

    /// <summary>
    /// Gets the HTTP route of the command, when the model has an executable model.
    /// </summary>
    public string? Route { get; init; }
}

/// <summary>
/// One input of a command a screen issues.
/// </summary>
/// <param name="Name">The input name.</param>
/// <param name="Type">The modeled type name, with <c>[]</c> for a list and <c>?</c> when optional.</param>
/// <param name="From">The screen value that fills it, such as <c>item.bookId</c>; null when the user enters it.</param>
public sealed record UiInput(string Name, string Type, string? From);

/// <summary>
/// A place a screen leads to.
/// </summary>
/// <param name="Screen">The screen or slice it leads to.</param>
/// <param name="Trigger">What leads there: <c>link</c>, <c>row</c>, <c>action</c>, <c>button</c> or <c>toolbar</c>.</param>
public sealed record UiNavigation(string Screen, string Trigger)
{
    /// <summary>
    /// Gets the screen value passed along, such as <c>item.bookId</c>, when one is.
    /// </summary>
    public string? By { get; init; }

    /// <summary>
    /// Gets how the target opens, such as <c>dialog</c>, <c>drawer</c> or a named slot, when the model says.
    /// </summary>
    public string? Opens { get; init; }
}

/// <summary>
/// How a screen is laid out.
/// </summary>
/// <param name="Fits">Where the screen fits: <c>page</c>, <c>dialog</c>, <c>drawer</c>, <c>pane</c> or <c>panel</c>; empty when the model leaves it open.</param>
/// <param name="Regions">The regions of the screen, outermost first.</param>
public sealed record UiLayout(IReadOnlyList<string> Fits, IReadOnlyList<UiRegion> Regions)
{
    /// <summary>
    /// Gets the name of the screen shape the screen follows, when it names one.
    /// </summary>
    public string? Template { get; init; }
}

/// <summary>
/// A region of a screen.
/// </summary>
/// <param name="Kind">The kind: <c>slot</c>, <c>section</c>, <c>list</c>, <c>details</c>, <c>embed</c>, <c>component</c> or <c>toolbar</c>.</param>
/// <param name="Regions">The regions inside it.</param>
public sealed record UiRegion(string Kind, IReadOnlyList<UiRegion> Regions)
{
    /// <summary>
    /// Gets the region name, when it has one.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Gets the read model a list or details region shows, or the screen an embed region shows.
    /// </summary>
    public string? Shows { get; init; }

    /// <summary>
    /// Gets the title shown in the region, when it has one.
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// Gets the commands whose actions are placed in the region.
    /// </summary>
    public IReadOnlyList<string> Actions { get; init; } = [];
}
