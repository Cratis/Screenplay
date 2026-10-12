// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json.Nodes;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces;

static class WorkspaceEventGenerations
{
    internal static void Append(JsonNode root, WorkspaceSyntaxEntry owner, uint revision)
    {
        var current = WorkspaceSyntaxMutation.At(root, owner.Handle.Path);
        var next = current.DeepClone();
        next["generation"] = revision;
        next["hasGenerationMarker"] = true;
        var separator = owner.Handle.Path.LastIndexOf('/');
        var collection = (JsonArray)WorkspaceSyntaxMutation.At(root, owner.Handle.Path[..separator]);
        collection[int.Parse(owner.Handle.Path[(separator + 1)..], System.Globalization.CultureInfo.InvariantCulture)] = WorkspaceSyntaxMutation.Json(owner.Node);
        collection.Add(next);
    }

    internal static ImmutableArray<WorkspacePropertyGeneration> Plan(IEnumerable<WorkspaceSyntaxEntry> owners, bool advance) =>
        [.. owners.Select(owner => new WorkspacePropertyGeneration(
            owner.Address!,
            ((EventSyntax)owner.Node).Generation,
            advance ? checked(((EventSyntax)owner.Node).Generation + 1) : ((EventSyntax)owner.Node).Generation,
            advance ? "newGeneration" : "eventNeverPersisted"))];
}
