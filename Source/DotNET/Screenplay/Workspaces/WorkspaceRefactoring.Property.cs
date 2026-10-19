// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces;

internal sealed partial class WorkspaceRefactoring
{
    internal WorkspaceAuthoringResult Property(WorkspacePropertyRequest request, WorkspacePropertyAssertionProvenance? assertions = null)
    {
        var report = new WorkspacePropertyReport { Action = request.Action };
        if (request.ExpectedRevision != workspace.Revision) return PropertyFailure(WorkspaceConflictKind.StaleWorkspaceRevision, report);
        if (request.ExpectedCatalogRevision != workspace.IdentityCatalog.Revision) return PropertyFailure(WorkspaceConflictKind.StaleCatalogRevision, report);
        if (!ValidPropertyOptions(request)) return PropertyFailure(WorkspaceConflictKind.InvalidOperation, report);

        try
        {
            return request.Action == "add" ? AddProperty(request, report) : PropertyCore(request, report, assertions);
        }
        catch (Exception exception) when (exception is InvalidWorkspaceAuthoring or InvalidSemanticContract)
        {
            return Failure(WorkspaceConflictKind.InvalidOperation, exception.Message) with { PropertyReport = report };
        }
    }

    static bool ValidPropertyOptions(WorkspacePropertyRequest request) => ValidPropertyMode(request) && ValidPropertySelectionArrays(request) && ValidPropertyAction(request);

    static bool ValidPropertyMode(WorkspacePropertyRequest request) => !request.NewGeneration || !request.EventNeverPersisted;
    static bool ValidPropertySelectionArrays(WorkspacePropertyRequest request) => !request.AlsoRename.IsDefault && !request.AlsoRemove.IsDefault;

    static bool ValidPropertyAction(WorkspacePropertyRequest request) => request.Action switch
    {
        "add" => ValidPropertyAddition(request),
        "rename" => ValidPropertyRename(request),
        "remove" => ValidPropertyRemoval(request),
        _ => false
    };

    static bool ValidPropertyAddition(WorkspacePropertyRequest request) => ValidPropertyAdditionOwner(request) && ValidPropertyAdditionShape(request) && NoOriginalPropertySelection(request) && !request.Hops.IsDefault;
    static bool ValidPropertyAdditionOwner(WorkspacePropertyRequest request) => request.Owner is not null && request.ExpectedOwnerName is not null;
    static bool ValidPropertyAdditionShape(WorkspacePropertyRequest request) => Identifier(request.Name) && !string.IsNullOrWhiteSpace(request.Type);
    static bool NoOriginalPropertySelection(WorkspacePropertyRequest request) => NoOriginalPropertyTarget(request) && NoPropertyRenameSelection(request) && request.AlsoRemove.IsEmpty;
    static bool NoOriginalPropertyTarget(WorkspacePropertyRequest request) => request.Target is null && request.ExpectedName is null;
    static bool NoPropertyRenameSelection(WorkspacePropertyRequest request) => request.NewName is null && request.AlsoRename.IsEmpty;

    static bool ValidPropertyRename(WorkspacePropertyRequest request) => ValidOriginalPropertySelection(request) && Identifier(request.NewName) && request.AlsoRemove.IsEmpty;
    static bool ValidPropertyRemoval(WorkspacePropertyRequest request) => ValidOriginalPropertySelection(request) && request.AlsoRename.IsEmpty && request.NewName is null;
    static bool ValidOriginalPropertySelection(WorkspacePropertyRequest request) => ValidOriginalPropertyTarget(request) && NoPropertyAdditionOptions(request);
    static bool ValidOriginalPropertyTarget(WorkspacePropertyRequest request) => request.Target is not null && request.ExpectedName is not null;
    static bool NoPropertyAdditionOptions(WorkspacePropertyRequest request) => NoPropertyAdditionOwner(request) && NoPropertyAdditionShape(request) && NoPropertyAdditionValues(request);
    static bool NoPropertyAdditionOwner(WorkspacePropertyRequest request) => request.Owner is null && request.ExpectedOwnerName is null;
    static bool NoPropertyAdditionShape(WorkspacePropertyRequest request) => request.Name is null && request.Type is null && !request.Optional;
    static bool NoPropertyAdditionValues(WorkspacePropertyRequest request) => request.Hops.IsEmpty && request.FixtureValue is null;

    static WorkspaceAuthoringResult PropertyFailure(WorkspaceConflictKind kind, WorkspacePropertyReport report, IEnumerable<WorkspacePropertyCandidate>? candidates = null)
    {
        var evidence = candidates?.Distinct().ToImmutableArray() ?? [];
        return new()
        {
            Conflicts =
            [
                new()
                {
                    Kind = kind,
                    Message = kind switch
                    {
                        WorkspaceConflictKind.EventContractChangeRequiresDecision => "EventContractChangeRequiresDecision: select newGeneration or eventNeverPersisted for the selected current event properties.",
                        WorkspaceConflictKind.BaselinedRevisionImmutable => "BaselinedRevisionImmutable: select newGeneration; it is the only option for the selected baselined revisions.",
                        _ => $"{kind}: property changes require a complete, safe selected-set plan."
                    },
                    Candidates = evidence
                }
            ],
            PropertyReport = report with { Generations = [], NeedsDecision = [new(kind.ToString(), report.Target, evidence)] }
        };
    }

    static WorkspaceSyntaxEntry[] PropertyEventOwners(IReadOnlyList<WorkspacePropertyPlan.PropertyMember> selected) =>
        [.. selected.Where(member => member.Owner.Node is EventSyntax).Select(member => member.Owner).DistinctBy(owner => owner.Address)
            .OrderBy(owner => owner.Address!, WorkspaceSemanticAddressComparer.Instance)];

    static WorkspacePropertyRefusal? InspectPropertySelections(WorkspacePropertyPlan plan, WorkspacePropertyRequest request, IReadOnlyList<WorkspacePropertyPlan.PropertyMember> selected)
    {
        var declarations = selected.Select(member => member.Declaration).ToHashSet();
        foreach (var member in selected)
        {
            if (WorkspacePropertySafety.Inspect(plan, member, request, declarations) is { } refusal) return refusal;
        }
        return null;
    }

    static WorkspaceConflictKind? EventDecision(WorkspaceSyntaxEntry[] events, WorkspacePropertyRequest request)
    {
        if (events.Length == 0) return null;
        if (request.ReleaseBaseline?.Status == "unreadable") return WorkspaceConflictKind.UnreadableReleaseBaseline;
        if (request.NewGeneration) return null;
        var bases = events.Select(owner => WorkspaceEventMutability.For(request.ReleaseBaseline, owner.EventContractId, ((EventSyntax)owner.Node).Generation)).ToArray();
        if (bases.Any(value => value.Basis == "baselined")) return WorkspaceConflictKind.BaselinedRevisionImmutable;
        return !request.EventNeverPersisted && bases.Any(value => value.Basis == "noBaseline") ? WorkspaceConflictKind.EventContractChangeRequiresDecision : null;
    }

    WorkspaceAuthoringResult PropertyCore(WorkspacePropertyRequest request, WorkspacePropertyReport report, WorkspacePropertyAssertionProvenance? assertions, ScreenplayWorkspace? origin = null)
    {
        var index = WorkspaceSyntaxIndex.Create(workspace);
        if (origin is null && InlinePropertyOwners(index, request) is { Length: > 0 } inline) return EvolveInlineProperty(request, report, index, inline);
        var plan = new WorkspacePropertyPlan(index);
        var selections = new WorkspacePropertySelections(index, plan, request, report);
        if (selections.Resolve() is { } selectionRefusal) return PropertyFailure(selectionRefusal.Kind, selections.Report, selectionRefusal.Candidates);
        report = selections.Report;
        if (InspectPropertySelections(plan, request, selections.Selected) is { } safetyRefusal) return PropertyFailure(safetyRefusal.Kind, report, safetyRefusal.Candidates);
        var events = PropertyEventOwners(selections.Selected);
        if (EventDecision(events, request) is { } decision) return PropertyFailure(decision, report, events.Select(owner => new WorkspacePropertyCandidate(owner.Address!, "Event", "declaration", owner.Location)));
        var generations = WorkspaceEventGenerations.Plan(events, request.NewGeneration, request.EventNeverPersisted, request.ReleaseBaseline);
        return ProposePropertyEdits(request, new WorkspacePropertyEdits(index, plan, selections.Selected, request, assertions), generations, report, origin);
    }

    // With an origin, the edits were planned on an in-memory extraction of it; propose them against the origin.
    WorkspaceAuthoringResult ProposePropertyEdits(WorkspacePropertyRequest request, WorkspacePropertyEdits edits, ImmutableArray<WorkspacePropertyGeneration> generations, WorkspacePropertyReport report, ScreenplayWorkspace? origin)
    {
        var changed = edits.Apply();
        if (edits.Refusal is { } assertionRefusal) return PropertyFailure(assertionRefusal.Kind, report, assertionRefusal.Candidates);
        var baseline = origin ?? workspace;
        var result = new WorkspaceAuthoringTransaction(baseline, edits.Renames).Propose(new()
        {
            ExpectedRevision = origin?.Revision ?? request.ExpectedRevision,
            ExpectedCatalogRevision = request.ExpectedCatalogRevision,
            ReleaseBaseline = request.ReleaseBaseline,
            Formatting = request.Formatting,
            Validation = request.Validation,
            Documents = WithExtractedDocuments(edits.Documents(generations), origin),
            SemanticRenames = [.. edits.Renames.Where(pair => workspace.IdentityCatalog.Semantics.Any(assignment => assignment.Address.Equals(pair.Key))).Select(pair => new SemanticIdentityRename(pair.Key, pair.Value))],
            RetiredSemanticAddresses = edits.Retirements,
            EventRevisionAdvancements = PropertyAdvancements(generations)
        });
        if (!result.Accepted)
        {
            return result with
            {
                PropertyReport = report with { NeedsDecision = [.. result.Conflicts.Select(conflict => new WorkspacePropertyDecision(conflict.Kind.ToString(), report.Target, conflict.Candidates))] }
            };
        }
        if (edits.InspectProjectionContinuity(result.Workspace!) is { } refusal) return PropertyFailure(refusal.Kind, report, refusal.Candidates);
        return result with
        {
            PropertyReport = WorkspacePropertyReports.Readiness(baseline, result.Workspace!, report with { Changed = changed, Generations = generations })
        };
    }

    ImmutableArray<EventContractRevisionAdvancement> PropertyAdvancements(ImmutableArray<WorkspacePropertyGeneration> generations) =>
        [.. generations.Where(generation => generation.Mode == "newGeneration" && workspace.IdentityCatalog.EventContracts.Any(assignment => assignment.Address.Equals(generation.Event)))
            .Select(generation => new EventContractRevisionAdvancement(generation.Event, new(generation.To)))];
}
