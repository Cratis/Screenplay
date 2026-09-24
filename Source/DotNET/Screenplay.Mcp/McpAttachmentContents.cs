// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

/// <summary>Refreshes optional attachment inputs from the trusted physical MCP root.</summary>
static class McpAttachmentContents
{
    internal static ScreenplayWorkspace Refresh(McpRoot root, ScreenplayWorkspace workspace)
    {
        var loaded = Load(root, workspace.Documents);
        if (workspace.AttachmentContents.Count == loaded.Contents.Count &&
            workspace.AttachmentContents.All(entry => loaded.Contents.TryGetValue(entry.Key, out var value) && value == entry.Value) &&
            workspace.AttachmentDiagnostics.SequenceEqual(loaded.Diagnostics))
        {
            return workspace;
        }

        return workspace.WithAttachmentContents(loaded.Contents, loaded.Diagnostics);
    }

    internal static AttachmentFileResult Load(McpRoot root, ImmutableArray<WorkspaceDocument> source)
    {
        var documents = source.Select(document => SemanticSourceDocument.Create(
            document.Id, document.StableKey, document.Path.Value, document.Text)).ToImmutableArray();
        return AttachmentFiles.Load(root.DirectoryPath, documents);
    }
}
