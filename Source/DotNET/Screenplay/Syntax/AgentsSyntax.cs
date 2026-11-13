// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Declares the complete allowlist of operations an agent may discover and call.
/// </summary>
/// <param name="Operations">The explicitly exposed operations, in authored order.</param>
/// <param name="Location">The location of the agents header.</param>
public record AgentsSyntax(IEnumerable<AgentOperationSyntax> Operations, SourceLocation Location) : SyntaxNode(Location)
{
    /// <summary>Gets the required token kind; absence never grants a default.</summary>
    public AgentTokensSyntax? Tokens { get; init; }
}
