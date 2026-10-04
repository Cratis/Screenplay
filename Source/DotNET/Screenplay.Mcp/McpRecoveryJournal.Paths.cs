// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

internal sealed partial class McpRecoveryJournal
{
    internal static IEnumerable<string> PlannedPaths(McpRoot root, IMcpProposal proposal, string operationId)
    {
        foreach (var name in new[] { McpState.FileName, FileName, $"{operationId}.stage", $"{operationId}.backup", $"{operationId}.journal" })
        {
            var path = new McpManagedFiles(root).PathFor(name);
            yield return path;
            if (!Directory.Exists(Path.GetDirectoryName(path))) yield return Path.GetDirectoryName(path)!;
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
