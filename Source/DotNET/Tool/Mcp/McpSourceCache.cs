// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Tool.Mcp;

sealed class McpSourceCache
{
    McpSnapshot? _snapshot;

    internal McpSnapshot Read(ImmutableArray<WorkspaceDocument> documents)
    {
        // Callers first read exact disk bytes; this never substitutes timestamps for source verification.
        var revision = McpSourceRevision.For(documents);
        if (_snapshot is null || _snapshot.SourceRevision != revision)
        {
            _snapshot = new(documents);
        }

        return _snapshot;
    }
}
