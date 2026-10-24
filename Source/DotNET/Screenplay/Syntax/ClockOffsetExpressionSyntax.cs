// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Represents a clock operand with an offset, such as <c>today - 7 days</c> or <c>now + 2 hours</c>.
/// </summary>
/// <param name="Clock">The clock keyword, <c>today</c> or <c>now</c>.</param>
/// <param name="Amount">The signed number of units added to the clock.</param>
/// <param name="Unit">The unit as written: <c>day</c>, <c>days</c>, <c>hour</c>, <c>hours</c>, <c>minute</c> or <c>minutes</c>.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
/// <remarks>
/// A bare <c>today</c> or <c>now</c> stays a <see cref="PathExpressionSyntax"/>. Decisions 0106 and 0107.
/// </remarks>
public record ClockOffsetExpressionSyntax(string Clock, int Amount, string Unit, SourceLocation Location) : ExpressionSyntax(Location);
