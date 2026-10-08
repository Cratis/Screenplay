// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Represents a refusal value available within a refusal branch's event mappings.
/// </summary>
/// <param name="Member">The refusal member: reason, constraint or message.</param>
/// <param name="Location">The source location of the expression.</param>
public record RefusalExpressionSyntax(string Member, SourceLocation Location) : ExpressionSyntax(Location);
