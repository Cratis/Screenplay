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
        if (request.Action is not ("rename" or "remove") || (request.NewGeneration && request.EventNeverPersisted) ||
            request.AlsoRename.IsDefault || request.AlsoRemove.IsDefault ||
            (request.Action == "rename" && (!Identifier(request.NewName) || !request.AlsoRemove.IsEmpty)) || (request.Action == "remove" && (!request.AlsoRename.IsEmpty || request.NewName is not null)))
        {
            return PropertyFailure(WorkspaceConflictKind.InvalidOperation, report);
        }

        try
        {
            return PropertyCore(request, report);
        }
        catch (Exception exception) when (exception is InvalidWorkspaceAuthoring or InvalidSemanticContract)
        {
            return Failure(WorkspaceConflictKind.InvalidOperation, exception.Message) with { PropertyReport = report };
        }
    }

    static WorkspaceAuthoringResult PropertyFailure(WorkspaceConflictKind kind, WorkspacePropertyReport report, IEnumerable<WorkspacePropertyCandidate>? candidates = null)
    {
        var evidence = candidates?.Distinct().ToImmutableArray() ?? [];
        return new()
        {
            Conflicts = [new() { Kind = kind, Message = kind == WorkspaceConflictKind.EventContractChangeRequiresDecision ? "EventContractChangeRequiresDecision: select newGeneration or eventNeverPersisted for the selected current event properties." : $"{kind}: property changes require a complete, safe selected-set plan.", Candidates = evidence }],
            PropertyReport = report with { Generations = [], NeedsDecision = [new(kind.ToString(), report.Target, evidence)] }
        };
    }

    WorkspaceAuthoringResult PropertyCore(WorkspacePropertyRequest request, WorkspacePropertyReport report)
    {
        var index = WorkspaceSyntaxIndex.Create(workspace);
        var plan = new WorkspacePropertyPlan(index);
        var selections = new[] { new WorkspacePropertySelection(request.Target, request.ExpectedName) }
            .Concat(request.Action == "rename" ? request.AlsoRename : request.AlsoRemove).ToArray();
        var selected = new List<WorkspacePropertyPlan.PropertyMember>();
        foreach (var selection in selections)
        {
            var entry = selection?.Target is { } handle ? index.Find(handle) : null;
            if (entry?.Node is not PropertySyntax property || property.Name != selection!.ExpectedName || plan.Member(entry) is not { } member)
            {
                return PropertyFailure(WorkspaceConflictKind.UnsupportedPropertySpan, report);
            }
            if (selection == selections[0]) report = report with { Target = member.Target };
            if (member.Owner.Member == "inlineEvent") return PropertyFailure(WorkspaceConflictKind.InlineEventRequiresExtraction, report);
            if (member.Owner.Node is EventSyntax @event && index.Entries.Any(other => member.Owner.Address!.Equals(other.Address) && other.Node is EventSyntax later && later.Generation > @event.Generation))
            {
                return PropertyFailure(WorkspaceConflictKind.HistoricalGenerationImmutable, report, [new(member.Entry.Address!, member.Kind, "declaration", entry.Location)]);
            }
            if (!selected.Contains(member)) selected.Add(member);
        }

        var chain = plan.Chain(selected[0].Declaration);
        var offChain = selected.Where(member => !chain.Contains(member.Declaration)).ToArray();
        if (offChain.Length > 0) return PropertyFailure(WorkspaceConflictKind.HopNotOnChain, report, offChain.Select(member => new WorkspacePropertyCandidate(member.Entry.Address!, member.Kind, "declaration", member.Entry.Location)));
        var untouched = plan.Members.Where(member => !chain.Contains(member.Declaration) && member.Declaration.Name == request.ExpectedName)
            .OrderBy(member => member.Entry.Address!, WorkspaceSemanticAddressComparer.Instance).Select(member => new WorkspacePropertyUnchanged(member.Entry.Address!, member.Kind, "offChain")).ToImmutableArray();
        report = report with
        {
            NotChanged = untouched,
            ChainCandidates = [.. chain.Where(declaration => !selected.Exists(member => member.Declaration == declaration)).Select(plan.Member).OfType<WorkspacePropertyPlan.PropertyMember>()
                .OrderBy(member => member.Entry.Address!, WorkspaceSemanticAddressComparer.Instance).Select(member => new WorkspacePropertyUnchanged(member.Entry.Address!, member.Kind, "notSelected", "property"))]
        };
        var selectedDeclarations = selected.Select(member => member.Declaration).ToHashSet();
        foreach (var member in selected)
        {
            if (WorkspacePropertySafety.Inspect(plan, member, request, selectedDeclarations) is { } refusal) return PropertyFailure(refusal.Kind, report, refusal.Candidates);
        }
        var events = selected.Where(member => member.Owner.Node is EventSyntax).Select(member => member.Owner).DistinctBy(owner => owner.Address)
            .OrderBy(owner => owner.Address!, WorkspaceSemanticAddressComparer.Instance).ToArray();
        if (events.Length > 0 && !request.NewGeneration && !request.EventNeverPersisted) return PropertyFailure(WorkspaceConflictKind.EventContractChangeRequiresDecision, report, events.Select(owner => new WorkspacePropertyCandidate(owner.Address!, "Event", "declaration", owner.Location)));

        var generations = WorkspaceEventGenerations.Plan(events, request.NewGeneration);
        var edits = new WorkspacePropertyEdits(index, plan, selected, request);
        var changed = edits.Apply();
        var result = new WorkspaceAuthoringTransaction(workspace, edits.Renames).Propose(new()
        {
            ExpectedRevision = request.ExpectedRevision,
            ExpectedCatalogRevision = request.ExpectedCatalogRevision,
            Formatting = request.Formatting,
            Validation = request.Validation,
            Documents = edits.Documents(generations),
            SemanticRenames = [.. edits.Renames.Where(pair => workspace.IdentityCatalog.Semantics.Any(assignment => assignment.Address.Equals(pair.Key))).Select(pair => new SemanticIdentityRename(pair.Key, pair.Value))],
            RetiredSemanticAddresses = edits.Retirements,
            EventRevisionAdvancements = [.. generations.Where(generation => generation.Mode == "newGeneration" && workspace.IdentityCatalog.EventContracts.Any(assignment => assignment.Address.Equals(generation.Event)))
                .Select(generation => new EventContractRevisionAdvancement(generation.Event, new(generation.To)))]
        });
        if (!result.Accepted)
        {
            return result with
            {
                PropertyReport = report with { NeedsDecision = [.. result.Conflicts.Select(conflict => new WorkspacePropertyDecision(conflict.Kind.ToString(), report.Target, conflict.Candidates))] }
            };
        }
        return result with
        {
            PropertyReport = WorkspacePropertyReports.Readiness(workspace, result.Workspace!, report with { Changed = changed, Generations = generations })
        };
    }
}
