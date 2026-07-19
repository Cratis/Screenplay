// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax.Projections;

/// <summary>
/// Represents the <c>$eventSourceId</c> expression - the identifier of the event source.
/// </summary>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record EventSourceIdExpressionSyntax(SourceLocation Location) : ExpressionSyntax(Location);

/// <summary>
/// Represents an <c>$eventContext</c> expression, such as <c>$eventContext.occurred</c>.
/// </summary>
/// <param name="Path">The dotted path following <c>$eventContext.</c>.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record EventContextExpressionSyntax(string Path, SourceLocation Location) : ExpressionSyntax(Location);

/// <summary>
/// Represents a <c>$causedBy</c> expression, optionally with a property such as <c>$causedBy.subject</c>.
/// </summary>
/// <param name="Property">The optional property of the identity that caused the event.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record CausedByExpressionSyntax(string? Property, SourceLocation Location) : ExpressionSyntax(Location);

/// <summary>
/// Represents a backtick template expression, such as <c>`${firstName} ${lastName}`</c>.
/// </summary>
/// <param name="Parts">The <see cref="TemplatePartSyntax">parts</see> of the template.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record TemplateExpressionSyntax(IEnumerable<TemplatePartSyntax> Parts, SourceLocation Location) : ExpressionSyntax(Location);

/// <summary>
/// Represents the base of a part of a template expression.
/// </summary>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public abstract record TemplatePartSyntax(SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents a verbatim text part of a template expression.
/// </summary>
/// <param name="Text">The verbatim text.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record TemplateTextSyntax(string Text, SourceLocation Location) : TemplatePartSyntax(Location);

/// <summary>
/// Represents an interpolated <c>${...}</c> part of a template expression.
/// </summary>
/// <param name="Expression">The interpolated <see cref="ExpressionSyntax"/>.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record TemplateInterpolationSyntax(ExpressionSyntax Expression, SourceLocation Location) : TemplatePartSyntax(Location);
