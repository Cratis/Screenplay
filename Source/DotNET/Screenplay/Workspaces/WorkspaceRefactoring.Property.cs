// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces;

internal sealed partial class WorkspaceRefactoring
{
    internal WorkspaceAuthoringResult Property(WorkspacePropertyRequest request)
    {
        var report = new WorkspacePropertyReport { Action = request.Action };
        if (request.ExpectedRevision != workspace.Revision) return PropertyFailure(WorkspaceConflictKind.StaleWorkspaceRevision, report);
        if (request.ExpectedCatalogRevision != workspace.IdentityCatalog.Revision) return PropertyFailure(WorkspaceConflictKind.StaleCatalogRevision, report);
        if (!ValidPropertyOptions(request)) return PropertyFailure(WorkspaceConflictKind.InvalidOperation, report);

        try
        {
            return PropertyCore(request, report);
        }
        catch (Exception exception) when (exception is InvalidWorkspaceAuthoring or InvalidSemanticContract)
        {
            return Failure(WorkspaceConflictKind.InvalidOperation, exception.Message) with { PropertyReport = report };
        }
    }

    static bool ValidPropertyOptions(WorkspacePropertyRequest request)
    {
        if (request.Action is not ("rename" or "remove")) return false;
        if (request.NewGeneration && request.EventNeverPersisted) return false;
        if (request.AlsoRename.IsDefault || request.AlsoRemove.IsDefault) return false;
        return request.Action == "rename" ? ValidPropertyRename(request) : ValidPropertyRemoval(request);
    }

    static bool ValidPropertyRename(WorkspacePropertyRequest request) => Identifier(request.NewName) && request.AlsoRemove.IsEmpty;
    static bool ValidPropertyRemoval(WorkspacePropertyRequest request) => request.AlsoRename.IsEmpty && request.NewName is null;

    static WorkspaceAuthoringResult PropertyFailure(WorkspaceConflictKind kind, WorkspacePropertyReport report, IEnumerable<WorkspacePropertyCandidate>? candidates = null)
    {
        var evidence = candidates?.Distinct().ToImmutableArray() ?? [];
        return new()
        {
            Conflicts = [new() { Kind = kind, Message = kind == WorkspaceConflictKind.EventContractChangeRequiresDecision ? "EventContractChangeRequiresDecision: select newGeneration or eventNeverPersisted for the selected current event properties." : $"{kind}: property changes require a complete, safe selected-set plan.", Candidates = evidence }],
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

    static bool EventDecisionRequired(WorkspaceSyntaxEntry[] events, WorkspacePropertyRequest request) => events.Length > 0 && !request.NewGeneration && !request.EventNeverPersisted;

    WorkspaceAuthoringResult PropertyCore(WorkspacePropertyRequest request, WorkspacePropertyReport report)
    {
        var index = WorkspaceSyntaxIndex.Create(workspace);
        var plan = new WorkspacePropertyPlan(index);
        var selections = new WorkspacePropertySelections(index, plan, request, report);
        if (selections.Resolve() is { } selectionRefusal) return PropertyFailure(selectionRefusal.Kind, selections.Report, selectionRefusal.Candidates);
        report = selections.Report;
        if (InspectPropertySelections(plan, request, selections.Selected) is { } safetyRefusal) return PropertyFailure(safetyRefusal.Kind, report, safetyRefusal.Candidates);
        var events = PropertyEventOwners(selections.Selected);
        if (EventDecisionRequired(events, request)) return PropertyFailure(WorkspaceConflictKind.EventContractChangeRequiresDecision, report, events.Select(owner => new WorkspacePropertyCandidate(owner.Address!, "Event", "declaration", owner.Location)));
        var generations = WorkspaceEventGenerations.Plan(events, request.NewGeneration);
        return ProposePropertyEdits(request, new WorkspacePropertyEdits(index, plan, selections.Selected, request), generations, report);
    }

    WorkspaceAuthoringResult ProposePropertyEdits(WorkspacePropertyRequest request, WorkspacePropertyEdits edits, ImmutableArray<WorkspacePropertyGeneration> generations, WorkspacePropertyReport report)
    {
        var changed = edits.Apply();
        if (edits.Refusal is { } assertionRefusal) return PropertyFailure(assertionRefusal.Kind, report, assertionRefusal.Candidates);
        var result = new WorkspaceAuthoringTransaction(workspace, edits.Renames).Propose(new()
        {
            ExpectedRevision = request.ExpectedRevision,
            ExpectedCatalogRevision = request.ExpectedCatalogRevision,
            Formatting = request.Formatting,
            Validation = request.Validation,
            Documents = edits.Documents(generations),
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
            PropertyReport = WorkspacePropertyReports.Readiness(workspace, result.Workspace!, report with { Changed = changed, Generations = generations })
        };
    }

    ImmutableArray<EventContractRevisionAdvancement> PropertyAdvancements(ImmutableArray<WorkspacePropertyGeneration> generations) =>
        [.. generations.Where(generation => generation.Mode == "newGeneration" && workspace.IdentityCatalog.EventContracts.Any(assignment => assignment.Address.Equals(generation.Event)))
            .Select(generation => new EventContractRevisionAdvancement(generation.Event, new(generation.To)))];
}
