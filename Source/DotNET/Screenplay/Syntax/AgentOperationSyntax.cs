// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>References an existing operation; exposure never declares or authorizes it.</summary>
/// <param name="Kind">The kind of operation being referenced.</param>
/// <param name="Reference">The bare or qualified reference as authored.</param>
/// <param name="Location">The location of the reference.</param>
public record AgentOperationSyntax(AgentOperationKind Kind, string Reference, SourceLocation Location) : SyntaxNode(Location)
{
    /// <summary>Gets the token scope required to call the operation.</summary>
    public AgentScope? Scope { get; init; }

    /// <summary>Gets the literal description agents use to select the operation.</summary>
    public string? Description { get; init; }
}
