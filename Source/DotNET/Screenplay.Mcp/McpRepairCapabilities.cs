// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Files;

namespace Cratis.Screenplay.Mcp;

static class McpRepairCapabilities
{
    static readonly string[] _actions = ["PLAY0166", "PLAY0478", "PLAY0563", "PLAY0564", "PLAY0614"];

    internal static object Read() => McpJson.ToolResult(new
    {
        schema = "cratis.screenplay.mcp.repair-capabilities",
        schemaVersion = 1,
        serverVersion = typeof(McpConnection).Assembly.GetName().Version!.ToString(),
        repairContractVersion = 1,
        actions = _actions.Select(code => new
        {
            diagnosticCode = code,
            tool = "propose-repair",
            requiredFormatting = "CanonicalizeTouchedDocuments",
            pinRepairEvidence = string.Equals(code, "PLAY0166", StringComparison.Ordinal) || string.Equals(code, "PLAY0478", StringComparison.Ordinal)
        }),
        evidence = new { version = 1, optional = true, revisionPrefix = "re1:", candidateResolutionChanges = "Refused", plannedWriteOverlap = "Refused", plannedWriteOverlapFailureKind = "RepairEvidenceWriteConflict" },
        structuredFailures = new { version = 1, discriminator = "failureKind", unknownApplyOutcome = "ApplyOutcomeUnknown" },
        savedFilesOnly = true,
        journaledApply = true,
        cancellation = new { supported = false, notifications = "Ignored", applyOutcomeAfterDisconnect = "Unknown" },
        limits = new
        {
            retainedProposals = 16,
            sourceFiles = McpRoot.MaximumFiles,
            sourceBytes = McpRoot.MaximumBytes,
            attachmentFileBytes = AttachmentFiles.MaximumFileBytes,
            attachmentBytes = AttachmentFiles.MaximumBytes,
            bytePageBytes = 192 * 1024,
            itemPageItems = 200,
            structuredResponseBytes = McpJson.MaximumStructuredResponseBytes,
            requestCharacters = McpConnection.MaximumRequestCharacters
        }
    });
}
