// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Defines the kinds of capture triggers.
/// </summary>
public enum CaptureWhenKind
{
    /// <summary>
    /// Append when a named property changes.
    /// </summary>
    PropertyChanged = 0,

    /// <summary>
    /// Append when an item appears in the source.
    /// </summary>
    Added = 1,

    /// <summary>
    /// Append when an item disappears from the source.
    /// </summary>
    Removed = 2,

    /// <summary>
    /// Append when an item changes in the source.
    /// </summary>
    Changed = 3
}

/// <summary>
/// Represents a <c>capture</c> declaration - translates external source data into events.
/// </summary>
/// <param name="Name">The name of the capture.</param>
/// <param name="Source">The <see cref="CaptureSourceSyntax"/> the capture reads from.</param>
/// <param name="Key">The source property identifying an instance.</param>
/// <param name="Map">The <see cref="CaptureMapSyntax">value mappings</see> applied before appending.</param>
/// <param name="Appends">The <see cref="CaptureAppendSyntax">appends</see> declared at the capture level.</param>
/// <param name="Children">The <see cref="CaptureChildrenSyntax">child collections</see> being captured.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record CaptureSyntax(
    string Name,
    CaptureSourceSyntax? Source,
    string? Key,
    IEnumerable<CaptureMapSyntax> Map,
    IEnumerable<CaptureAppendSyntax> Appends,
    IEnumerable<CaptureChildrenSyntax> Children,
    SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents the <c>source</c> of a capture, with its settings.
/// </summary>
/// <param name="Kind">The kind of source, such as <c>api</c>.</param>
/// <param name="Settings">The <see cref="CaptureSourceSettingSyntax">settings</see> of the source.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record CaptureSourceSyntax(string Kind, IEnumerable<CaptureSourceSettingSyntax> Settings, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents a single setting of a capture source, such as <c>route /invoices</c> or <c>poll 5m</c>.
/// </summary>
/// <param name="Name">The name of the setting.</param>
/// <param name="Value">The verbatim value of the setting.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record CaptureSourceSettingSyntax(string Name, string Value, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents a <c>map</c> entry translating source values, such as <c>status = status translate</c>.
/// </summary>
/// <param name="Property">The target property.</param>
/// <param name="Source">The source property.</param>
/// <param name="Translations">The <see cref="CaptureTranslationSyntax">value translations</see>.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record CaptureMapSyntax(
    string Property,
    string Source,
    IEnumerable<CaptureTranslationSyntax> Translations,
    SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents a single value translation, such as <c>"utkast" =&gt; draft</c>.
/// </summary>
/// <param name="From">The source value being translated.</param>
/// <param name="To">The target value.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record CaptureTranslationSyntax(string From, string To, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents an <c>append</c> declaration - the event to append when the source changes.
/// </summary>
/// <param name="Event">The name of the event to append.</param>
/// <param name="When">The <see cref="CaptureWhenSyntax"/> describing when to append.</param>
/// <param name="Mappings">The <see cref="PropertyMappingSyntax">property mappings</see> for the appended event.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record CaptureAppendSyntax(
    string Event,
    CaptureWhenSyntax? When,
    IEnumerable<PropertyMappingSyntax> Mappings,
    SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents the <c>when</c> trigger of an append.
/// </summary>
/// <param name="Kind">The <see cref="CaptureWhenKind"/> of the trigger.</param>
/// <param name="Property">The property whose change triggers the append, when <see cref="CaptureWhenKind.PropertyChanged"/>.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record CaptureWhenSyntax(CaptureWhenKind Kind, string? Property, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents a <c>children</c> block within a capture - changes in a child collection.
/// </summary>
/// <param name="Property">The child collection property.</param>
/// <param name="IdentifiedBy">The property identifying a child instance.</param>
/// <param name="Appends">The <see cref="CaptureAppendSyntax">appends</see> for the child collection.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record CaptureChildrenSyntax(
    string Property,
    string IdentifiedBy,
    IEnumerable<CaptureAppendSyntax> Appends,
    SourceLocation Location) : SyntaxNode(Location);
