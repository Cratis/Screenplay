// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Represents <c>date of &lt;operand&gt;</c>, the calendar date of an instant in the application time zone.
/// </summary>
/// <param name="Operand">The <see cref="ExpressionSyntax"/> the date is taken of.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record DateOfExpressionSyntax(ExpressionSyntax Operand, SourceLocation Location) : ExpressionSyntax(Location);
