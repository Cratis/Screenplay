// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

// Loads the sources semantic-diff compares: the open workspace, or a model folder read without writing anything.
internal sealed partial class McpWorkspaces
{
    internal (ScreenplayWorkspace Workspace, bool HasPersistedIdentities) ReadComparisonWorkspace()
    {
        if (_workspace is null)
        {
            throw new McpFailure("Open a workspace first.");
        }

        return (Current(), _stateBytes is not null);
    }

    internal (ScreenplayWorkspace Workspace, bool HasPersistedIdentities) ReadComparisonPath(string path)
    {
        var root = ResolveRequestedRoot(path);
        McpRecoveryJournal.RefusePending(root);
        var files = new McpManagedFiles(root);
        var persisted = files.Read(McpState.FileName);
        var state = persisted is null ? null : McpState.Deserialize(persisted);
        var workspace = state?.Open(root) ?? OpenFromDisk(root, root.ApplicationName);
        root.Verify(workspace);
        files.Verify(McpState.FileName, persisted);
        McpRecoveryJournal.RefusePending(root);

        return (workspace, persisted is not null);
    }

    static ScreenplayWorkspace OpenFromDisk(McpRoot root, string name)
    {
        var documents = root.Read(allowEmpty: true);
        var identity = ApplicationIdentity.Create(name);
        if (documents.IsEmpty)
        {
            return ScreenplayWorkspace.CreateEmpty(identity, name);
        }

        var loaded = McpAttachmentContents.Load(root, documents);

        return ScreenplayWorkspace.Create(identity, name, documents, SemanticIdentityCatalog.Empty(identity), loaded.Contents, loaded.Diagnostics);
    }

    McpRoot ResolveRequestedRoot(string path)
    {
        var requested = new McpRoot(Path.GetFullPath(path, CurrentDirectoryHint ?? Environment.CurrentDirectory));

        if (_configuredRoot is null)
        {
            return requested;
        }

        // Check the approved root before touching an old worktree that may have been removed.
        return McpDirectoryIdentity.Same(_configuredRoot, requested)
            ? _configuredRoot : McpWorktreeRoots.Resolve(_configuredRoot, requested);
    }
}
