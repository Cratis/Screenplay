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
                    if (OperatingSystem.IsWindows() && input.Suffix.Length > 0 && target.Suffix.Length == 0 && !Directory.Exists(write))
                    {
                        // A replaced file can acquire a new short name even when its old entry has none.
                        // Existing parent directories are not recreated; compare the future leaf at that anchor.
                        var parent = Inspect(root, Path.GetDirectoryName(write)!) ?? throw Uncertain(reference);
                        if (McpDirectoryIdentity.SameEntry(input.Existing, parent.Existing) &&
                            MissingNamesMayAlias(input.Suffix, Path.GetFileName(write), windows: true))
                        {
                            throw Uncertain(reference);
                        }
                    }

                    if (input.Suffix.Length == 0 && target.Suffix.Length == 0)
                    {
                        if (McpDirectoryIdentity.SameEntry(input.Existing, target.Existing)) throw Conflict(reference, write);
                    }
                    else if (McpDirectoryIdentity.SameEntry(input.Existing, target.Existing))
                    {
                        if (input.Suffix.Equals(target.Suffix, StringComparison.Ordinal)) throw Conflict(reference, write);

                        // Missing names cannot be resolved by stat. Similar spellings are uncertainty, not an
                        // invented OS-wide case rule (Linux and macOS both support different volume semantics).
                        if (MissingNamesMayAlias(input.Suffix, target.Suffix, OperatingSystem.IsWindows()))
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

    // The boolean is a deterministic comparison seam, not a request option. Existing entries always
    // use filesystem identity above. Missing Windows names have no short-name metadata to prove separation.
    internal static bool MissingNamesMayAlias(string input, string target, bool windows)
    {
        var inputs = Comparable(input).Split('/');
        var targets = Comparable(target).Split('/');
        if (inputs.Length != targets.Length) return false;
        for (var index = 0; index < inputs.Length; index++)
        {
            if (inputs[index].Equals(targets[index], StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (windows && ((PlausibleShortAlias(inputs[index]) && !DosName(targets[index])) ||
                            (PlausibleShortAlias(targets[index]) && !DosName(inputs[index]))))
            {
                continue;
            }

            return false;
        }

        return true;
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

    static bool PlausibleShortAlias(string segment)
    {
        if (!DosName(segment, shortAliasCandidate: true)) return false;
        var name = segment.Split('.')[0];
        var tilde = name.LastIndexOf('~');

        return tilde > 0 && tilde < name.Length - 1 && name[(tilde + 1)..].All(char.IsAsciiDigit);
    }

    static bool DosName(string segment, bool shortAliasCandidate = false)
    {
        var parts = segment.Split('.');

        return parts.Length <= 2 && parts[0].Length is > 0 and <= 8 &&
            (parts.Length == 1 || parts[1].Length is > 0 and <= 3) &&
            parts.All(part => part.All(character => char.IsAsciiLetterOrDigit(character) || "$%'-_@~`!(){}^#&".Contains(character) ||
                (shortAliasCandidate && character > 127)));
    }

    static string Comparable(string path) => string.Join('/', path.Normalize(NormalizationForm.FormC).Split('/').Select(segment => segment.TrimEnd(' ', '.')));

    static McpFailure Conflict(string reference, string write) => new($"Pinned repair attachment '{reference}' overlaps planned write '{write}'; select a repair with disjoint attachment inputs.") { FailureKind = "RepairEvidenceWriteConflict" };

    static McpFailure Uncertain(string reference) => new($"Cannot prove pinned repair attachment '{reference}' is disjoint from planned writes without resolving an uncertain filesystem alias; no proposal can be accepted.") { FailureKind = "RepairEvidenceWriteConflict" };

    sealed record Location(string Existing, string Suffix);
}
