// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>Declares the token kind required for agent calls.</summary>
/// <param name="Kind">The declared token kind.</param>
/// <param name="Location">The location of the declaration.</param>
public record AgentTokensSyntax(AgentTokenKind Kind, SourceLocation Location) : SyntaxNode(Location);
