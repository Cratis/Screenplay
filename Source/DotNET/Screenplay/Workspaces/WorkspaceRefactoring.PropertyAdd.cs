// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces;

internal sealed partial class WorkspaceRefactoring
{
    static WorkspacePropertyRefusal? InspectAddedProperties(ScreenplayWorkspace candidate, WorkspacePropertyAddPlan plan)
    {
        var after = new WorkspacePropertyPlan(WorkspaceSyntaxIndex.Create(candidate));
        var members = after.Members.Where(member => plan.Additions.Any(addition => member.Declaration.Address == $"{addition.AuthoredAddress}.{addition.Name}")).ToArray();
        var links = members.SelectMany(member => after.Links.Incoming(member.Declaration).Select(link => (Member: member, Link: link))).Where(item => item.Link.Resolution != "resolved").ToArray();
        if (links.Length == 0) return null;
        var kind = links.Any(item => item.Link.Reference.Role.Equals("opaque", StringComparison.Ordinal) || item.Link.Reference.Role.Equals("unclassified", StringComparison.Ordinal)) ? WorkspaceConflictKind.OpaquePropertyReference : WorkspaceConflictKind.AmbiguousPropertyReference;
        return new(kind, [.. links.Select(item => new WorkspacePropertyCandidate(item.Member.Entry.Address!, item.Member.Kind, item.Link.Reference.Role, item.Link.Reference.Location)).Distinct()]);
    }

    WorkspaceAuthoringResult AddProperty(WorkspacePropertyRequest request, WorkspacePropertyReport report)
    {
        var index = WorkspaceSyntaxIndex.Create(workspace);
        var properties = new WorkspacePropertyPlan(index);
        var plan = new WorkspacePropertyAddPlan(index, properties, request, report);
        if (plan.Resolve() is { } refusal) return PropertyFailure(refusal.Kind, plan.Report, refusal.Candidates);
        report = plan.Report;
        var events = plan.Additions.Where(addition => addition.Owner.Node is EventSyntax).Select(addition => addition.Owner).DistinctBy(owner => owner.Address)
            .OrderBy(owner => owner.Address!, WorkspaceSemanticAddressComparer.Instance).ToArray();
        if (EventDecision(events, request) is { } decision) return PropertyFailure(decision, report, plan.Additions.Where(addition => addition.Owner.Node is EventSyntax).Select(WorkspacePropertyAddPlan.Candidate));
        if (plan.InspectSources() is { } sourceRefusal) return PropertyFailure(sourceRefusal.Kind, report, sourceRefusal.Candidates);
        var generations = WorkspaceEventGenerations.Plan(events, request.NewGeneration, request.EventNeverPersisted, request.ReleaseBaseline);
        var edits = new WorkspacePropertyAddEdits(index, properties, plan, request);
        if (edits.Apply() is { } editRefusal) return PropertyFailure(editRefusal.Kind, report, editRefusal.Candidates);
        var result = new WorkspaceAuthoringTransaction(workspace, edits.Renames).Propose(new()
        {
            ExpectedRevision = request.ExpectedRevision,
            ExpectedCatalogRevision = request.ExpectedCatalogRevision,
            ReleaseBaseline = request.ReleaseBaseline,
            Formatting = request.Formatting,
            Validation = request.Validation,
            Documents = edits.Documents(generations),
            SemanticRenames = [.. edits.Renames.Select(pair => new SemanticIdentityRename(pair.Key, pair.Value))],
            EventRevisionAdvancements = PropertyAdvancements(generations)
        });
        if (!result.Accepted) return result with { PropertyReport = report with { NeedsDecision = [.. result.Conflicts.Select(conflict => new WorkspacePropertyDecision(conflict.Kind.ToString(), report.Target, conflict.Candidates))] } };
        if (InspectAddedProperties(result.Workspace!, plan) is { } coverageRefusal) return PropertyFailure(coverageRefusal.Kind, report, coverageRefusal.Candidates);
        if (new WorkspacePropertyProjectionContinuity(properties, request, [], plan.Additions).Inspect(result.Workspace!) is { } continuityRefusal) return PropertyFailure(continuityRefusal.Kind, report, continuityRefusal.Candidates);
        return result with { PropertyReport = WorkspacePropertyReports.Readiness(workspace, result.Workspace!, edits.Describe(result.Workspace!, report) with { Generations = generations }) };
    }
}
