// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces;

internal sealed partial class WorkspaceRefactoring
{
    static void RequireStreamPartTarget(WorkspaceSyntaxIndex index, WorkspaceSyntaxEntry target, string newName)
    {
        if (target.Node is not EventStreamIdPartSyntax part) return;
        var stream = target.Parent is { } parent ? index.Find(parent) : null;
        var source = stream?.Parent is { } sourceParent ? index.Find(sourceParent) : null;
        if (stream?.Node is not EventStreamSyntax declaration || source?.Node is not EventSourceSyntax owner ||
            index.Entries.Count(entry => entry.Node is EventSourceSyntax candidate && candidate.Name == owner.Name) != 1 ||
            owner.Streams.Count(candidate => candidate.Name == declaration.Name) != 1 ||
            declaration.StreamIdParts.Count(candidate => candidate.Name == part.Name) != 1)
        {
            throw new InvalidWorkspaceAuthoring("Cannot prove a stream id part rename without one physical source, stream and part declaration.");
        }

        if (declaration.StreamIdParts.Any(candidate => candidate.Name == newName && candidate.Name != part.Name))
        {
            throw new InvalidWorkspaceAuthoring($"Stream '{owner.Name}.{declaration.Name}' already declares stream id part '{newName}'.");
        }
    }
}
