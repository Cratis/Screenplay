// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Indexing;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Comparison.for_ModelComparison;

public class when_classifying_sample_references : Cratis.Screenplay.for_Samples.given.the_sample_workspaces
{
    readonly List<string> _missing = [];
    readonly List<string> _renameDisagreements = [];
    readonly List<string> _unindexedInteractions = [];
    int _checked;
    int _renameMembers;
    int _interactions;

    void Because()
    {
        foreach (var (name, workspace) in _workspaces)
        {
            var index = WorkspaceSyntaxIndex.Create(workspace);
            foreach (var entry in index.Entries) CheckEntry(name, entry, index);
            foreach (var operand in WorkspaceReferenceMembers.All(index).Where(operand => IsInteraction(operand.Entry.Node))) CheckInteraction(name, operand);
            _checked++;
        }
    }

    void CheckEntry(string name, WorkspaceSyntaxEntry entry, WorkspaceSyntaxIndex index)
    {
        if (!ReferenceKinds.Covers(entry.Node, null)) _missing.Add($"{name}: {entry.Kind}");
        foreach (var (member, _) in WorkspaceReferenceMembers.Members(entry, index).Where(member => !IsLocal(member.Domain)))
        {
            var property = char.ToUpperInvariant(member[0]) + member[1..];
            if (!ReferenceKinds.IsReference(entry.Node, property)) _renameDisagreements.Add($"{name}: {entry.Kind}.{property}");
            _renameMembers++;
        }
    }

    void CheckInteraction(string name, WorkspaceReferenceMember operand)
    {
        if (!ReferenceKinds.For(operand.Entry.Node).Any(reference => reference.Name == operand.Text)) _unindexedInteractions.Add($"{name}: {operand.Entry.Kind}.{operand.Member}");
        _interactions++;
    }

    // Local property paths, case parameters and stream-id parts are intentionally not declaration edges.
    static bool IsLocal(WorkspaceReferenceDomain domain) => domain is WorkspaceReferenceDomain.Property or WorkspaceReferenceDomain.SpecificationParameter or WorkspaceReferenceDomain.EventStreamIdPart;

    static bool IsInteraction(SyntaxNode node) => node is ExecuteCommandActionSyntax or RefreshQueryActionSyntax or NavigateActionSyntax or OpenDialogActionSyntax or RaiseTriggerActionSyntax or BehaviorArgumentSyntax;

    [Fact] void should_check_the_current_samples() => (_checked >= 10).ShouldBeTrue();
    [Fact] void should_cover_every_sample_syntax_member() => _missing.ShouldBeEmpty();
    [Fact] void should_share_reference_decisions_with_the_rename_helpers() => _renameDisagreements.ShouldBeEmpty();
    [Fact] void should_check_real_rename_operands() => (_renameMembers > 100).ShouldBeTrue();
    [Fact] void should_index_the_interactions_the_rename_helpers_recognize() => _unindexedInteractions.ShouldBeEmpty();
    [Fact] void should_check_real_interactions() => (_interactions > 10).ShouldBeTrue();
}
