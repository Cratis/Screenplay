// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax.Captures;

/// <summary>
/// Represents a <c>capture</c> declaration - translates external source data into events.
/// </summary>
/// <param name="Name">The name of the capture.</param>
/// <param name="Source">The <see cref="CaptureSourceSyntax"/> the capture reads from.</param>
/// <param name="Key">The source property identifying an instance.</param>
/// <param name="Map">The <see cref="CaptureMapOperationSyntax">value mappings</see> applied before appending.</param>
/// <param name="Appends">The <see cref="CaptureAppendSyntax">appends</see> declared at the capture level.</param>
/// <param name="Children">The <see cref="CaptureChildrenSyntax">child collections</see> being captured.</param>
/// <param name="Nested">The <see cref="CaptureNestedSyntax">nested objects</see> being captured.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record CaptureSyntax(
    string Name,
    CaptureSourceSyntax? Source,
    string? Key,
    IEnumerable<CaptureMapOperationSyntax> Map,
    IEnumerable<CaptureAppendSyntax> Appends,
    IEnumerable<CaptureChildrenSyntax> Children,
    IEnumerable<CaptureNestedSyntax> Nested,
    SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents the <c>source</c> of a capture, with its settings.
/// </summary>
/// <param name="Kind">The kind of source, such as <c>api</c>, <c>webhook</c> or <c>message</c>.</param>
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
