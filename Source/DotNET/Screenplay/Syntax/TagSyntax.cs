// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Represents a <c>tag</c> line on an event, a <c>produces</c> declaration or a capture <c>append</c>.
/// </summary>
/// <param name="Value">
/// The <see cref="ExpressionSyntax"/> providing the tag value - a <see cref="LiteralExpressionSyntax"/> for
/// static tags, or a context or environment expression for tags resolved dynamically.
/// </param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record TagSyntax(ExpressionSyntax Value, SourceLocation Location) : SyntaxNode(Location);
