// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring;

public class when_renaming_a_concept_and_its_cross_file_references : given.an_authoring_workspace
{
    WorkspaceAuthoringResult _result = null!;
    WorkspaceSyntaxEntry _concept = null!;
    SemanticAddress _renamed = null!;
    WorkspaceAuthoringRequest _request = null!;

    void Establish()
    {
        _concept = Index.Entries.Single(entry => entry.Node is ConceptSyntax concept && concept.Name == "ProjectName");
        _renamed = SemanticAddress.ForConcept(Workspace.IdentityCatalog.Application, "ProjectTitle");
        var references = Index.Entries.Where(entry => entry.Node is TypeRefSyntax reference && reference.Name == "ProjectName");
        _request = Authoring(
        [
            new ReplaceWorkspaceNode(_concept.Handle, _concept.Node, (ConceptSyntax)_concept.Node with { Name = "ProjectTitle" }),
            .. references.Select(entry => new ReplaceWorkspaceNode(entry.Handle, entry.Node, (TypeRefSyntax)entry.Node with { Name = "ProjectTitle" }))
        ]) with
        { SemanticRenames = [new(_concept.Address, _renamed)] };
    }

    void Because() => _result = Workspace.ProposeAuthoring(_request);

    [Fact] void should_validate_the_complete_final_state_only() => _result.Accepted.ShouldBeTrue();
    [Fact] void should_be_executable_after_coordinated_reference_edits() => _result.ExecutableReady.ShouldBeTrue();
    [Fact] void should_preserve_the_assigned_identity() => _result.Workspace!.IdentityCatalog.ResolveSemantic(_renamed).ShouldEqual(_concept.SemanticId!.Value);
    [Fact] void should_write_both_documents() => _result.WritePlan.Entries.Length.ShouldEqual(2);
    [Fact] void should_reject_the_same_rename_without_migration() => Workspace.ProposeAuthoring(_request with { SemanticRenames = [] }).Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.InvalidIdentityMigration);
}
