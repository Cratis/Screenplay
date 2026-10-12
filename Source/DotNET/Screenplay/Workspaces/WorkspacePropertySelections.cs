// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces;

sealed class WorkspacePropertySelections(WorkspaceSyntaxIndex index, WorkspacePropertyPlan plan, WorkspacePropertyRequest request, WorkspacePropertyReport report)
{
    readonly List<WorkspacePropertyPlan.PropertyMember> _selected = [];

    internal IReadOnlyList<WorkspacePropertyPlan.PropertyMember> Selected => _selected;
    internal WorkspacePropertyReport Report { get; private set; } = report;

    internal WorkspacePropertyRefusal? Resolve()
    {
        var first = new WorkspacePropertySelection(request.Target!, request.ExpectedName!);
        var additional = request.Action == "rename" ? request.AlsoRename : request.AlsoRemove;
        foreach (var selection in new[] { first }.Concat(additional))
        {
            if (Select(selection, first) is { } refusal) return refusal;
        }
        return ResolveChain();
    }

    static bool ExpectedProperty(WorkspaceSyntaxEntry? entry, WorkspacePropertySelection? selection) => entry?.Node is PropertySyntax property && property.Name == selection!.ExpectedName;

    WorkspacePropertyRefusal? Select(WorkspacePropertySelection? selection, WorkspacePropertySelection first)
    {
        var entry = selection?.Target is { } handle ? index.Find(handle) : null;
        if (!ExpectedProperty(entry, selection)) return new(WorkspaceConflictKind.UnsupportedPropertySpan, []);
        var member = plan.Member(entry!);
        if (member is null) return new(WorkspaceConflictKind.UnsupportedPropertySpan, []);
        if (selection == first) Report = Report with { Target = member.Target };
        if (member.Owner.Member == "inlineEvent") return new(WorkspaceConflictKind.InlineEventRequiresExtraction, []);
        if (plan.Historical(member.Owner)) return new(WorkspaceConflictKind.HistoricalGenerationImmutable, [member.DeclarationCandidate]);
        if (!_selected.Contains(member)) _selected.Add(member);
        return null;
    }

    WorkspacePropertyRefusal? ResolveChain()
    {
        var chain = plan.Chain(_selected[0].Declaration);
        var offChain = _selected.Where(member => !chain.Contains(member.Declaration)).ToArray();
        if (offChain.Length > 0) return new(WorkspaceConflictKind.HopNotOnChain, [.. offChain.Select(member => member.DeclarationCandidate)]);
        Report = Report with
        {
            NotChanged = [.. plan.Members.Where(member => !chain.Contains(member.Declaration) && member.Declaration.Name == request.ExpectedName)
                .OrderBy(member => member.Entry.Address!, WorkspaceSemanticAddressComparer.Instance).Select(member => new WorkspacePropertyUnchanged(member.Entry.Address!, member.Kind, "offChain"))],
            ChainCandidates = [.. chain.Where(declaration => !_selected.Exists(member => member.Declaration == declaration)).Select(plan.Member).OfType<WorkspacePropertyPlan.PropertyMember>()
                .OrderBy(member => member.Entry.Address!, WorkspaceSemanticAddressComparer.Instance).Select(member => new WorkspacePropertyUnchanged(member.Entry.Address!, member.Kind, "notSelected", "property"))]
        };
        return null;
    }
}
