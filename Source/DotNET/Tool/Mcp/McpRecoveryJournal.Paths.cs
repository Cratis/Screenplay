// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Tool.Mcp;

internal sealed partial class McpRecoveryJournal
{
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

    string Artifact(WorkspaceDocument document, string kind)
    {
        var path = $"{_root.PathFor(document.Path)}.screenplay-mcp-{Record.OperationId}.{kind}";
        if (Encoding.UTF8.GetByteCount(Path.GetFileName(path)) > 255)
        {
            throw new McpFailure($"RecoveryPathLimit: shorten '{document.Path}' before applying; its recovery filename exceeds the portable filesystem limit.");
        }

        McpManagedFiles.CheckExisting(path);
        return path;
    }
}
