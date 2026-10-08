// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Represents an ordered refusal branch on a reaction's command invocation.
/// </summary>
/// <param name="Selector">The refusal category: any, validation, constraint or authorization. Any excludes authorization.</param>
/// <param name="Constraint">The optional constraint name narrowing a constraint refusal.</param>
/// <param name="Acknowledge">Whether the branch acknowledges the refusal without producing events.</param>
/// <param name="Produces">The events the branch produces instead of acknowledging.</param>
/// <param name="Location">The source location of the branch.</param>
public record InvocationRefusalSyntax(
    string Selector,
    string? Constraint,
    bool Acknowledge,
    IEnumerable<ProducesSyntax> Produces,
    SourceLocation Location) : SyntaxNode(Location);
