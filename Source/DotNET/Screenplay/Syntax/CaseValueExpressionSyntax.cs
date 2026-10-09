// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// References a whole parameter value in a specification table step.
/// </summary>
/// <param name="Parameter">The referenced parameter.</param>
/// <param name="Location">The value reference location.</param>
public record CaseValueExpressionSyntax(string Parameter, SourceLocation Location) : ExpressionSyntax(Location);
