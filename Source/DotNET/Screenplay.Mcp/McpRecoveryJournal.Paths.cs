// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

internal sealed partial class McpRecoveryJournal
{
    internal string StateRestoreStage => OperationPath(_root, Record.OperationId, "state-rollback");

    internal static IEnumerable<string> PlannedPaths(McpRoot root, IMcpProposal proposal, string operationId)
    {
        foreach (var path in new[]
        {
            new McpManagedFiles(root).PathFor(McpState.FileName),
            new McpManagedFiles(root).PathFor(FileName),
            OperationPath(root, operationId, "stage"),
            OperationPath(root, operationId, "backup"),
            OperationPath(root, operationId, "journal"),
            OperationPath(root, operationId, "state-rollback")
        })
        {
            yield return path;
            if (!Directory.Exists(Path.GetDirectoryName(path))) yield return Path.GetDirectoryName(path)!;
        }

        // Rollback visits every original, including unchanged documents (and restores their access rules).
        for (var index = 0; index < proposal.Before.Documents.Length; index++)
        {
            yield return root.PathFor(proposal.Before.Documents[index].Path);
            yield return RestorePath(root, operationId, index);
        }

        foreach (var entry in Differences(proposal.Before, proposal.Workspace))
        {
            if (entry.Before is not null)
            {
                yield return root.PathFor(entry.Before.Path);
                yield return Artifact(root, entry.Before, operationId, "backup");
            }

            if (entry.After is not null)
            {
                var path = root.PathFor(entry.After.Path);
                yield return path;
                for (var parent = Path.GetDirectoryName(path); parent is not null && !Directory.Exists(parent); parent = Path.GetDirectoryName(parent))
                {
                    yield return parent;
                }

                yield return Artifact(root, entry.After, operationId, "stage");
            }
        }
    }

    internal static string OperationPath(McpRoot root, string operationId, string kind, bool create = false) =>
        new McpManagedFiles(root).PathFor($"{operationId}.{kind}", create);

    internal static string RestorePath(McpRoot root, string operationId, int index) =>
        new McpManagedFiles(root).PathFor($"{operationId}-{index}.rollback");

    internal string RestoreStage(int index) => RestorePath(_root, Record.OperationId, index);

    static IEnumerable<WorkspaceWriteEntry> Differences(ScreenplayWorkspace before, ScreenplayWorkspace after)
    {
        var originals = before.Documents.ToDictionary(document => document.Id);
        var candidates = after.Documents.ToDictionary(document => document.Id);
        foreach (var id in originals.Keys.Union(candidates.Keys).OrderBy(id => id.ToString(), StringComparer.Ordinal))
        {
            originals.TryGetValue(id, out var original);
            candidates.TryGetValue(id, out var candidate);
            if (original is null || candidate is null || original.StableKey != candidate.StableKey || original.Path != candidate.Path ||
                !original.Bytes.AsSpan().SequenceEqual(candidate.Bytes.AsSpan()))
            {
                var kind = (original, candidate) switch
                {
                    (null, _) => WorkspaceWriteKind.Added,
                    (_, null) => WorkspaceWriteKind.Removed,
                    _ => WorkspaceWriteKind.Replaced
                };
                yield return new() { Document = id, Kind = kind, Before = original, After = candidate };
            }
        }
    }

    static string Artifact(McpRoot root, WorkspaceDocument document, string operationId, string kind)
    {
        var path = $"{root.PathFor(document.Path)}.screenplay-mcp-{operationId}.{kind}";
        if (Encoding.UTF8.GetByteCount(Path.GetFileName(path)) > 255)
        {
            throw new McpFailure($"RecoveryPathLimit: shorten '{document.Path}' before applying; its recovery filename exceeds the portable filesystem limit.");
        }

        McpManagedFiles.CheckExisting(path);
        return path;
    }

    string Artifact(WorkspaceDocument document, string kind) => Artifact(_root, document, Record.OperationId, kind);
}
