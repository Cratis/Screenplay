// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Files;

namespace Cratis.Screenplay.Mcp;

static class McpRepairCapabilities
{
    const string Canonical = "CanonicalizeTouchedDocuments";
    const string PreserveTrivia = "PreserveTrivia";

    // Each row mirrors what the repair itself declares: its formatting, whether it may join a fix-all, and the
    // consequences an editor must state before consent. A row that cannot be fixed all requires individual review.
    static readonly McpRepairAction[] _actions =
    [
        new("PLAY0166", Canonical, CanFixAll: true, PinRepairEvidence: true),
        new("PLAY0469", Canonical, CanFixAll: false, ChangesEventContract: true, RetiresSemanticAddresses: true),
        new("PLAY0478", Canonical, CanFixAll: false, ChangesRouting: true, PinRepairEvidence: true),
        new("PLAY0516", Canonical, CanFixAll: true),
        new("PLAY0563", Canonical, CanFixAll: false),
        new("PLAY0564", Canonical, CanFixAll: false),
        new("PLAY0614", Canonical, CanFixAll: false),
        new("PLAY0653", PreserveTrivia, CanFixAll: true),
        new("PLAY0661", Canonical, CanFixAll: true),
        new("PLAY0664", Canonical, CanFixAll: true),
        new("PLAY0683", Canonical, CanFixAll: false),
        new("PLAY0684", Canonical, CanFixAll: false),
        new("PLAY0688", Canonical, CanFixAll: false),
        new("PLAY0701", Canonical, CanFixAll: false),
        new("PLAY0786", Canonical, CanFixAll: false),
        new("PLAY0787", Canonical, CanFixAll: true)
    ];

    internal static object Read() => McpJson.ToolResult(new
    {
        schema = "cratis.screenplay.mcp.repair-capabilities",
        schemaVersion = 1,
        serverVersion = typeof(McpConnection).Assembly.GetName().Version!.ToString(),
        repairContractVersion = 1,
        actions = _actions.Select(action => new
        {
            diagnosticCode = action.DiagnosticCode,
            tool = "propose-repair",
            requiredFormatting = action.RequiredFormatting,
            pinRepairEvidence = action.PinRepairEvidence,
            canFixAll = action.CanFixAll,
            individualReviewRequired = !action.CanFixAll,
            changesEventContract = action.ChangesEventContract,
            changesRouting = action.ChangesRouting,
            retiresSemanticAddresses = action.RetiresSemanticAddresses
        }),
        intentActions = new[]
        {
            new
            {
                diagnosticCode = "PLAY0470", tool = "propose-repair", intentSchema = "destinationIntent", intentVersion = 1,
                discoveryView = "repair-intents", reportView = "production-destinations",
                requiredFormatting = "CanonicalizeTouchedDocuments", requiredValidation = "Executable",
                individualReviewRequired = true, baselineExecutableAvailable = false, preservationProven = false,
                canFixAll = false, pinRepairEvidence = false
            }
        },
        refactorings = new object[]
        {
            new
            {
                kind = "extract-inline-event", tool = "propose-extract-inline-event", discoveryView = "refactorings",
                requiredFormatting = Canonical, canFixAll = false, individualReviewRequired = true,
                changesEventContract = false, changesRouting = false, retiresSemanticAddresses = false, pinRepairEvidence = false
            },
            new
            {
                kind = "rename-declaration", tool = "propose-rename", discoveryView = "refactorings",
                requiredFormatting = PreserveTrivia, canFixAll = false, individualReviewRequired = true,
                eventPinDefault = "Preserve", eventNeverPersistedOptIn = true, pinRepairEvidence = false
            }
        },
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

/// <summary>
/// Describes one advertised no-input diagnostic repair and the consequences an editor states before consent.
/// </summary>
/// <param name="DiagnosticCode">The repaired diagnostic code.</param>
/// <param name="RequiredFormatting">The formatting consent the proposal requires.</param>
/// <param name="CanFixAll">Whether the repair may be applied together with others of the same code.</param>
/// <param name="ChangesEventContract">Whether the repair changes the shape of an existing event contract.</param>
/// <param name="ChangesRouting">Whether the repair changes where a production appends.</param>
/// <param name="RetiresSemanticAddresses">Whether the repair can retire assigned semantic addresses.</param>
/// <param name="PinRepairEvidence">Whether the proposal supports pinned repair evidence v1.</param>
sealed record McpRepairAction(
    string DiagnosticCode,
    string RequiredFormatting,
    bool CanFixAll,
    bool ChangesEventContract = false,
    bool ChangesRouting = false,
    bool RetiresSemanticAddresses = false,
    bool PinRepairEvidence = false);
