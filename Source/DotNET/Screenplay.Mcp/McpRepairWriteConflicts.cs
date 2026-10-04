// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Mcp;

// Inspects only the loader's model-selected manifest and the server-owned write plan. Never reads file bytes,
// follows links, probes arbitrary locks, creates parents, or writes a case-sensitivity test file.
internal static class McpRepairWriteConflicts
{
    internal static void Verify(McpRoot root, IMcpProposal proposal, string operationId)
    {
        var sources = proposal.Before.Documents.Concat(proposal.Workspace.Documents)
            .Select(document => SemanticSourceDocument.Create(document.Id, document.StableKey, document.Path.Value, document.Text)).ToImmutableArray();
        var references = AttachmentFiles.References(sources).Select(reference => reference.Path).Distinct(StringComparer.Ordinal);
        var writes = McpRecoveryJournal.PlannedPaths(root, proposal, operationId).Distinct(StringComparer.Ordinal).ToArray();
        foreach (var reference in references)
        {
            // Invalid/outside-root references are refused by the loader and cannot become admitted inputs.
            if (!AttachmentFiles.TryNormalize(reference, out var key, out _)) continue;
            var path = Path.Combine(root.DirectoryPath, key);
            foreach (var write in writes)
            {
                if (path.Equals(write, StringComparison.Ordinal)) throw Conflict(reference, write);
            }

            try
            {
                var input = Inspect(root, path);

                // A link-refused reference remains loader evidence, but is never followed to find aliases.
                if (input is null) continue;
                foreach (var write in writes)
                {
                    var target = Inspect(root, write) ?? throw Uncertain(reference);
                    if (input.Suffix.Length == 0 && target.Suffix.Length == 0)
                    {
                        if (McpDirectoryIdentity.SameEntry(input.Existing, target.Existing)) throw Conflict(reference, write);
                    }
                    else if (McpDirectoryIdentity.SameEntry(input.Existing, target.Existing))
                    {
                        if (input.Suffix.Equals(target.Suffix, StringComparison.Ordinal)) throw Conflict(reference, write);

                        // Missing names cannot be resolved by stat. Similar spellings are uncertainty, not an
                        // invented OS-wide case rule (Linux and macOS both support different volume semantics).
                        if (Comparable(input.Suffix).Equals(Comparable(target.Suffix), StringComparison.OrdinalIgnoreCase))
                        {
                            throw Uncertain(reference);
                        }
                    }
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                throw Uncertain(reference);
            }
            catch (McpFailure failure) when (failure.FailureKind != "RepairEvidenceWriteConflict")
            {
                throw Uncertain(reference);
            }
        }
    }

    static Location? Inspect(McpRoot root, string path)
    {
        var current = root.DirectoryPath;
        var segments = Path.GetRelativePath(current, path).Split(Path.DirectorySeparatorChar);
        for (var index = 0; index < segments.Length; index++)
        {
            var next = Path.Combine(current, segments[index]);
            try
            {
                if (File.GetAttributes(next).HasFlag(FileAttributes.ReparsePoint)) return null;
                current = next;
            }
            catch (FileNotFoundException)
            {
                McpRoot.CheckAncestors(current);
                return new(current, string.Join('/', segments[index..]));
            }
            catch (DirectoryNotFoundException)
            {
                McpRoot.CheckAncestors(current);
                return new(current, string.Join('/', segments[index..]));
            }
        }

        McpRoot.CheckAncestors(current);
        return new(current, string.Empty);
    }

    static string Comparable(string path) => string.Join('/', path.Normalize(NormalizationForm.FormC).Split('/').Select(segment => segment.TrimEnd(' ', '.')));

    static McpFailure Conflict(string reference, string write) => new($"Pinned repair attachment '{reference}' overlaps planned write '{write}'; select a repair with disjoint attachment inputs.") { FailureKind = "RepairEvidenceWriteConflict" };

    static McpFailure Uncertain(string reference) => new($"Cannot prove pinned repair attachment '{reference}' is disjoint from planned writes without resolving an uncertain filesystem alias; no proposal can be accepted.") { FailureKind = "RepairEvidenceWriteConflict" };

    sealed record Location(string Existing, string Suffix);
}
