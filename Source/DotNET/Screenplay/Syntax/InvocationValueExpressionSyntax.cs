// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Represents a value an ability invocation makes available: <c>$result.&lt;field&gt;</c>, <c>$outcome</c>,
/// <c>$failure.reason</c> or a workflow's <c>$trigger.&lt;value&gt;</c>.
/// </summary>
/// <param name="Source">The value source: <c>result</c>, <c>outcome</c>, <c>failure</c> or <c>trigger</c>.</param>
/// <param name="Member">The member read from the source, empty when the source is read whole.</param>
/// <param name="Location">The source location of the expression.</param>
public record InvocationValueExpressionSyntax(string Source, string Member, SourceLocation Location) : ExpressionSyntax(Location);
