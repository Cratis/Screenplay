// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Mcp;

sealed record McpRootBindingConflict(string BoundRoot, ImmutableArray<string> StateRoots, ImmutableArray<string> PendingRoots)
{
    public string Kind => "WorkspaceRootConflict";
    public string Message => $"Multiple workspace roots hold .screenplay/identities.json or .screenplay/pending.json. Bound root: '{BoundRoot}'. Competing state roots (nearest the client-offered root first): {string.Join(", ", StateRoots.Select(path => $"'{path}'"))}. Pending journal roots: {string.Join(", ", PendingRoots.Select(path => $"'{path}'"))}. No state was migrated. Inspect each root with an explicit open-workspace path before choosing which workspace to keep; recover a competing journal only after opening its root explicitly.";
}
