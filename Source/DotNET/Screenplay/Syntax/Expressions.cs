// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Represents the base of every expression in the syntax tree.
/// </summary>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public abstract record ExpressionSyntax(SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents a literal value expression - a string, number, boolean or null.
/// </summary>
/// <param name="Value">The literal value, <c>null</c> for the <c>null</c> literal.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record LiteralExpressionSyntax(object? Value, SourceLocation Location) : ExpressionSyntax(Location);

/// <summary>
/// Represents a dotted property path expression, such as <c>customer.name</c>.
/// </summary>
/// <param name="Path">The dotted path.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record PathExpressionSyntax(string Path, SourceLocation Location) : ExpressionSyntax(Location);

/// <summary>
/// Represents a <c>$context</c> expression, such as <c>$context.occurred</c> or <c>$context.identity.id</c>.
/// </summary>
/// <param name="Path">The dotted path following <c>$context.</c>.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record ContextExpressionSyntax(string Path, SourceLocation Location) : ExpressionSyntax(Location);

/// <summary>
/// Represents an <c>$env</c> expression referencing an environment variable, such as <c>$env.SERVICE_NAME</c>.
/// </summary>
/// <param name="Name">The name of the environment variable.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record EnvironmentExpressionSyntax(string Name, SourceLocation Location) : ExpressionSyntax(Location);

/// <summary>
/// Represents a freeform expression captured verbatim, such as an arithmetic or method-call expression.
/// </summary>
/// <param name="Text">The verbatim expression text.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record RawExpressionSyntax(string Text, SourceLocation Location) : ExpressionSyntax(Location);

/// <summary>
/// Represents a <c>$.</c> expression referencing a property of the current source item in a capture, such as <c>$.id</c>.
/// </summary>
/// <param name="Path">The dotted path following <c>$.</c>.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record SourceItemExpressionSyntax(string Path, SourceLocation Location) : ExpressionSyntax(Location);
