// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp;

sealed record McpReferenceEdge(McpReference Reference, McpDeclaration[] Targets)
{
    internal string Resolution => (Reference.AmbiguousSourceOwner, Reference.IncompleteSourceOwner, Targets.Length) switch
    {
        (true, _, _) => "ambiguous",
        (_, true, _) => "incomplete",
        (_, _, 0) => "unresolved",
        (_, _, 1) => Reference.Kinds.Contains(Targets[0].Kind, StringComparer.Ordinal) ? "resolved" : "wrongKind",
        _ => "ambiguous"
    };
}
