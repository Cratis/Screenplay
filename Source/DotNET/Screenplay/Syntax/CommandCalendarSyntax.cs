// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Represents a command <c>calendar "&lt;id&gt;"</c> or <c>calendar from $context.&lt;member&gt;</c> declaration.
/// </summary>
/// <param name="Id">The IANA time zone id as written, or <c>null</c> when the calendar comes from the context.</param>
/// <param name="ContextMember">The <c>$context</c> member the calendar comes from, or <c>null</c> when an id is written.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record CommandCalendarSyntax(string? Id, string? ContextMember, SourceLocation Location) : SyntaxNode(Location);
