// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Ui;

/// <summary>
/// The stack-neutral UI contract of a model: what every screen shows, which commands it issues, where it leads
/// and how it is laid out, with Scene presentation as an optional layer.
/// </summary>
/// <param name="Contract">The contract name and version, <see cref="Version"/>.</param>
/// <param name="Application">The application name.</param>
/// <param name="Screens">The screens, in the order their slices declare them.</param>
public sealed record UiContract(string Contract, string Application, IReadOnlyList<UiScreen> Screens)
{
    /// <summary>
    /// The contract name and version this library writes.
    /// </summary>
    public const string Version = "screenplay-ui/1";

    /// <summary>
    /// Gets the revision of the executable model the contract's identities and routes come from, or null when the
    /// model has no executable model.
    /// </summary>
    public string? ModelRevision { get; init; }
}

/// <summary>
/// One screen of a slice.
/// </summary>
/// <param name="Name">The screen name.</param>
/// <param name="Slice">The declaring slice as <c>Module.Feature[.Feature...].Slice</c>.</param>
/// <param name="Title">The title shown to a user; the screen name when the model gives none.</param>
/// <param name="Data">The data the screen shows.</param>
/// <param name="Actions">The commands the screen issues.</param>
/// <param name="Navigation">Where the screen leads.</param>
/// <param name="Layout">How the screen is laid out.</param>
public sealed record UiScreen(
    string Name,
    string Slice,
    string Title,
    IReadOnlyList<UiData> Data,
    IReadOnlyList<UiAction> Actions,
    IReadOnlyList<UiNavigation> Navigation,
    UiLayout Layout)
{
    /// <summary>
    /// Gets the semantic identity of the declaring slice in the executable model, when there is one.
    /// </summary>
    public string? SliceId { get; init; }

    /// <summary>
    /// Gets the screen's description, when the model gives one.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the Scene presentation of the screen, when it uses Scene vocabulary. A builder for another stack may ignore it.
    /// </summary>
    public UiScene? Scene { get; init; }
}

/// <summary>
/// Data a screen shows: the result of one query.
/// </summary>
/// <param name="Query">The query name.</param>
/// <param name="ReadModel">The read model the query returns.</param>
/// <param name="Many">Whether the query returns a list rather than one instance.</param>
/// <param name="Fields">The read-model properties the screen shows with their labels; empty when it shows them all.</param>
public sealed record UiData(string Query, string ReadModel, bool Many, IReadOnlyList<UiField> Fields)
{
    /// <summary>
    /// Gets the screen value the query is narrowed by, such as <c>bookId</c>, when it is.
    /// </summary>
    public string? By { get; init; }

    /// <summary>
    /// Gets the semantic identity of the query, when the model has an executable model.
    /// </summary>
    public string? QueryId { get; init; }

    /// <summary>
    /// Gets the HTTP route of the query, when the model has an executable model.
    /// </summary>
    public string? Route { get; init; }

    /// <summary>
    /// Gets whether the query is observable, so the screen updates when the data changes.
    /// </summary>
    public bool Live { get; init; }
}

/// <summary>
/// A read-model property a screen shows.
/// </summary>
/// <param name="Property">The property name.</param>
/// <param name="Label">The label shown with it, when the model gives one.</param>
public sealed record UiField(string Property, string? Label);
