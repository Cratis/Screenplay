// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces;

static class WorkspaceEventGenerations
{
    internal static ImmutableArray<WorkspacePropertyGeneration> Plan(IEnumerable<WorkspaceSyntaxEntry> owners, bool advance) =>
        [.. owners.Select(owner => new WorkspacePropertyGeneration(
            owner.Address!,
            ((EventSyntax)owner.Node).Generation,
            advance ? checked(((EventSyntax)owner.Node).Generation + 1) : ((EventSyntax)owner.Node).Generation,
            advance ? "newGeneration" : "eventNeverPersisted"))];
}
