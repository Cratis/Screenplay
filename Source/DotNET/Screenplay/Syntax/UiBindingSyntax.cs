// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Defines the typed source of a UI binding.
/// </summary>
public enum UiBindingKind
{
    /// <summary>
    /// The binding text could not be parsed as a supported typed binding.
    /// </summary>
    Invalid = 0,

    /// <summary>
    /// The binding reads from the inherited data context.
    /// </summary>
    DataContext = 1,

    /// <summary>
    /// The binding reads from a named query result.
    /// </summary>
    QueryResult = 2,

    /// <summary>
    /// The binding reads from another component's exposed property.
    /// </summary>
    ComponentProperty = 3
}

/// <summary>
/// Defines how binding updates flow.
/// </summary>
public enum UiBindingMode
{
    /// <summary>
    /// Source changes update the target.
    /// </summary>
    OneWay = 0,

    /// <summary>
    /// Source and target changes flow both ways.
    /// </summary>
    TwoWay = 1
}

/// <summary>
/// Defines how null source values affect the target.
/// </summary>
public enum UiBindingNullBehavior
{
    /// <summary>
    /// Propagate null to the target.
    /// </summary>
    Propagate = 0,

    /// <summary>
    /// Clear the target.
    /// </summary>
    Clear = 1,

    /// <summary>
    /// Preserve the target value.
    /// </summary>
    Preserve = 2
}

/// <summary>
/// Represents a typed UI binding source compatible with Scene's binding contract.
/// </summary>
/// <param name="BindingKind">The binding source kind.</param>
/// <param name="Path">The path inside the source.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the binding starts in source text.</param>
public record UiBindingSyntax(UiBindingKind BindingKind, string Path, SourceLocation Location) : SyntaxNode(Location)
{
    /// <summary>
    /// Gets the named query when <see cref="BindingKind"/> is <see cref="UiBindingKind.QueryResult"/>.
    /// </summary>
    public string? Query { get; init; }

    /// <summary>
    /// Gets the stable component instance id when <see cref="BindingKind"/> is <see cref="UiBindingKind.ComponentProperty"/>.
    /// </summary>
    public string? ComponentId { get; init; }

    /// <summary>
    /// Gets the component property path when it differs from <see cref="Path"/>.
    /// </summary>
    public string? ComponentPropertyPath { get; init; }

    /// <summary>
    /// Gets the requested binding update mode.
    /// </summary>
    public UiBindingMode? Mode { get; init; }

    /// <summary>
    /// Gets the requested null behavior.
    /// </summary>
    public UiBindingNullBehavior? NullBehavior { get; init; }

    /// <summary>
    /// Gets the expected value type used by downstream validation.
    /// </summary>
    public string? ExpectedValueType { get; init; }

    /// <summary>
    /// Gets the raw authored binding text for invalid bindings.
    /// </summary>
    public string? RawText { get; init; }
}
