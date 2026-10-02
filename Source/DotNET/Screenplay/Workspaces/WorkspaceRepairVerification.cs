// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Workspaces;

internal static class WorkspaceRepairVerification
{
    // Weak keys are snapshot-local, including attachments. Values contain no candidates or write plans.
    // Each key comes from an actual diagnostic and occurrence, so storage is bounded by the snapshot.
    static readonly ConditionalWeakTable<ScreenplayWorkspace, Verification> _verification = [];

    internal static int TransactionCount(ScreenplayWorkspace workspace) => _verification.GetOrCreateValue(workspace).Transactions;

    internal static WorkspaceAuthoringResult Propose(ScreenplayWorkspace workspace, WorkspaceAuthoringRequest request)
    {
        Interlocked.Increment(ref _verification.GetOrCreateValue(workspace).Transactions);
        return workspace.ProposeAuthoring(request);
    }

    internal static WorkspaceAuthoringResult Verify(WorkspaceSyntaxIndex index, WorkspaceDiagnosticRepair repair, WorkspaceAuthoringRequest request)
    {
        var result = VerifyTransaction(index, repair, request);
        if (request.Documents is { IsDefault: false, Length: 0 } && request.SemanticRenames is { IsDefault: false, Length: 0 } &&
            request.EventRenames is { IsDefault: false, Length: 0 } && request.RetiredSemanticAddresses is { IsDefault: false, Length: 0 } &&
            request.RetiredEventAddresses is { IsDefault: false, Length: 0 } && Enum.IsDefined(request.Validation) && Enum.IsDefined(request.ReferencePolicy))
        {
            _verification.GetOrCreateValue(index.Workspace).Subjects.GetOrAdd(
                (repair.Subject, repair.DiagnosticCode, request.Validation, request.ReferencePolicy),
                static (_, value) => new Lazy<Verdict>(value),
                Verdict.From(result));
        }

        return result;
    }

    internal static ImmutableArray<WorkspaceDiagnosticRepair> Discover(WorkspaceSyntaxIndex index, WorkspaceDiagnosticRepair repair, bool verifyRepair)
    {
        if (!verifyRepair)
        {
            return [repair];
        }

        var workspace = index.Workspace;
        var request = new WorkspaceAuthoringRequest
        {
            ExpectedRevision = workspace.Revision,
            ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
            Validation = WorkspaceAuthoringValidation.Authoring,
            Formatting = repair.RequiredFormatting
        };
        var verdict = _verification.GetOrCreateValue(workspace).Subjects.GetOrAdd(
            (repair.Subject, repair.DiagnosticCode, request.Validation, request.ReferencePolicy),
            static (_, state) => new Lazy<Verdict>(() => Verdict.From(VerifyTransaction(state.Index, state.Repair, state.Request))),
            (Index: index, Repair: repair, Request: request)).Value;

        return verdict.Accepted ? [repair] : [];
    }

    internal static WorkspaceAuthoringResult RequireComments(WorkspaceAuthoringResult result)
    {
        if (result.Accepted && !WorkspaceDroppedComments.In(result.WritePlan!).IsEmpty)
        {
            return Refuse(result, WorkspaceConflictKind.RepairWouldDropComments, "The proposal would drop comments from the touched document.");
        }

        return result;
    }

    internal static WorkspaceAuthoringResult Refuse(WorkspaceAuthoringResult result, WorkspaceConflictKind kind, string message) => result with
    {
        Workspace = null,
        WritePlan = null,
        Conflicts = [new WorkspaceConflict { Kind = kind, Message = message }]
    };

    internal static bool SameModel(ScreenplayWorkspace before, ScreenplayWorkspace after) =>
        before.Compilation.Value is { } original && after.Compilation.Value is { } candidate &&
        SemanticModelSerializer.Serialize(original.Model).AsSpan().SequenceEqual(SemanticModelSerializer.Serialize(candidate.Model));

    static WorkspaceAuthoringResult VerifyTransaction(WorkspaceSyntaxIndex index, WorkspaceDiagnosticRepair repair, WorkspaceAuthoringRequest request)
    {
        var result = RequireComments(Propose(index.Workspace, request with
        {
            Operations = repair.Operations,
            RetiredSemanticAddresses = request.RetiredSemanticAddresses.IsDefault ? default : [.. request.RetiredSemanticAddresses, .. repair.RetiredSemanticAddresses]
        }));
        if (result.Accepted && repair.DiagnosticCode == DiagnosticCodes.EventSourceIdInPayload &&
            (WorkspaceEventRepairs.HasConsumers(index, repair) || !result.ExecutableReady ||
                !WorkspaceProductionRepairs.KeepsDestinations(index, index.Find(index.Find(repair.Subject)!.Parent!)!, result, true, true)))
        {
            return Refuse(result, WorkspaceConflictKind.InvalidOperation, "Removing the payload property changes the event contract. Consumer, opaque implementation or routing impact cannot be proven safe.");
        }
        if (result.Accepted && repair.DiagnosticCode == DiagnosticCodes.OmittedProductionDestination &&
            !WorkspaceProductionRepairs.KeepsOtherDestinations(index, index.Find(repair.Subject)!, result))
        {
            return Refuse(result, WorkspaceConflictKind.InvalidOperation, "The diagnostic repair would change language or semantic version, or another production's destination.");
        }

        if (result.Accepted && repair.DiagnosticCode == DiagnosticCodes.RedundantEventId &&
            (!SameModel(index.Workspace, result.Workspace!) || index.Workspace.IdentityCatalog.Revision != result.Workspace!.IdentityCatalog.Revision))
        {
            return Refuse(result, WorkspaceConflictKind.InvalidOperation, "Cannot prove that removing the redundant id preserves the executable model and catalog.");
        }

        return result;
    }

    sealed record Verdict(bool Accepted, ImmutableArray<WorkspaceConflict> Conflicts)
    {
        internal static Verdict From(WorkspaceAuthoringResult result) => new(result.Accepted, result.Accepted ? [] : result.Conflicts);
    }

    sealed class Verification
    {
        internal int Transactions;
        internal ConcurrentDictionary<(WorkspaceNodeHandle Subject, string Code, WorkspaceAuthoringValidation Validation, WorkspaceAuthoringReferencePolicy ReferencePolicy), Lazy<Verdict>> Subjects { get; } = [];
    }
}
