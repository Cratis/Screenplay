// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax.Captures;

/// <summary>
/// Represents a <c>children</c> block within a capture - changes in a child collection.
/// </summary>
/// <param name="Property">The child collection property.</param>
/// <param name="IdentifiedBy">The property identifying a child instance.</param>
/// <param name="Map">The <see cref="CaptureMapOperationSyntax">value mappings</see> applied to each child before appending.</param>
/// <param name="Appends">The <see cref="CaptureAppendSyntax">appends</see> for the child collection.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record CaptureChildrenSyntax(
    string Property,
    string IdentifiedBy,
    IEnumerable<CaptureMapOperationSyntax> Map,
    IEnumerable<CaptureAppendSyntax> Appends,
    SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents a <c>nested</c> block within a capture - a single nullable child object.
/// </summary>
/// <param name="Property">The nested object property.</param>
/// <param name="Map">The <see cref="CaptureMapOperationSyntax">value mappings</see> applied to the nested object before appending.</param>
/// <param name="Appends">The <see cref="CaptureAppendSyntax">appends</see> for the nested object.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record CaptureNestedSyntax(
    string Property,
    IEnumerable<CaptureMapOperationSyntax> Map,
    IEnumerable<CaptureAppendSyntax> Appends,
    SourceLocation Location) : SyntaxNode(Location);
