// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

// Uses the authoritative loader's full inputs and refusal diagnostics, not bound requirements or lock files.
// AttachmentFiles already bounds bytes, validates UTF-8 and refuses links. No unrelated files are read.
internal sealed record McpRepairEvidence(string BeforeRevision, string CandidateRevision)
{
    // Chosen by the server at retention, never accepted from tool arguments or exposed as evidence.
    internal string OperationId { get; } = Guid.NewGuid().ToString("N");

    internal static string Revision(ScreenplayWorkspace workspace)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        void Add(object value)
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(value, McpJson.Options);
            hash.AppendData(Encoding.UTF8.GetBytes(bytes.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) + ":"));
            hash.AppendData(bytes);
        }

        Add("screenplay-repair-evidence-v1");
        Add(workspace.Revision.ToString());
        Add(workspace.IdentityCatalog.Revision.ToString());
        foreach (var entry in workspace.AttachmentContents.OrderBy(entry => entry.Key, StringComparer.Ordinal))
        {
            Add(entry.Key);
            Add(entry.Value);
        }

        foreach (var diagnostic in workspace.AttachmentDiagnostics)
        {
            Add(diagnostic);
        }

        return "re1:" + Convert.ToHexStringLower(hash.GetHashAndReset());
    }

    internal static string? Expected(JsonElement arguments)
    {
        var revision = McpJson.OptionalString(arguments, "expectedRepairEvidenceRevision");
        if (revision is not null && (revision.Length != 68 || !revision.StartsWith("re1:", StringComparison.Ordinal) ||
            revision.Skip(4).Any(character => !char.IsAsciiHexDigitLower(character))))
        {
            throw new McpFailure("expectedRepairEvidenceRevision must be an opaque repair evidence v1 revision.", -32602);
        }

        return revision;
    }

    internal static void Check(string? expected, ScreenplayWorkspace workspace)
    {
        if (expected is not null && expected != Revision(workspace))
        {
            throw Drift();
        }
    }

    internal static McpRepairEvidence Pin(McpRoot root, IMcpProposal proposal)
    {
        var evidence = new McpRepairEvidence(Revision(proposal.Before), Revision(proposal.Workspace));

        // A candidate's loader result must equal the exact inputs used by the selected validation.
        // Refuse a different candidate resolution rather than refreshing readiness without repeating proof.
        evidence.Verify(root, proposal);
        return evidence;
    }

    internal void Verify(McpRoot root, IMcpProposal proposal)
    {
        McpRepairWriteConflicts.Verify(root, proposal, OperationId);
        Check(BeforeRevision, McpAttachmentContents.Refresh(root, proposal.Before));
        Check(CandidateRevision, McpAttachmentContents.Refresh(root, proposal.Workspace));
    }

    static McpFailure Drift() => new("Repair evidence changed; rediscover and validate a fresh selected repair.") { FailureKind = "RepairEvidenceDrift" };
}
