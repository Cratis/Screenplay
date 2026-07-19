// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax.Projections;

/// <summary>
/// Represents the base of a <c>key</c> declaration identifying a read model instance.
/// </summary>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public abstract record KeySyntax(SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents a key given by a single expression, such as <c>key invoiceId</c> or <c>key "global"</c>.
/// </summary>
/// <param name="Expression">The <see cref="ExpressionSyntax"/> providing the key.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record ExpressionKeySyntax(ExpressionSyntax Expression, SourceLocation Location) : KeySyntax(Location);

/// <summary>
/// Represents a composite key built from multiple parts, such as <c>key OrderKey</c> with indented parts.
/// </summary>
/// <param name="Type">The name of the composite key type.</param>
/// <param name="Parts">The <see cref="KeyPartSyntax">parts</see> of the key.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record CompositeKeySyntax(string Type, IEnumerable<KeyPartSyntax> Parts, SourceLocation Location) : KeySyntax(Location);

/// <summary>
/// Represents a single part of a composite key.
/// </summary>
/// <param name="Property">The property of the key type.</param>
/// <param name="Expression">The <see cref="ExpressionSyntax"/> providing the value.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record KeyPartSyntax(string Property, ExpressionSyntax Expression, SourceLocation Location) : SyntaxNode(Location);
