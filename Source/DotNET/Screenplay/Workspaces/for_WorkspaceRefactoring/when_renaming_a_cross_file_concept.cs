// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_renaming_a_cross_file_concept : given.a_refactoring_workspace
{
    WorkspaceAuthoringResult _result = null!;
    SemanticId _identity;

    void Establish() => _identity = Workspace.IdentityCatalog.ResolveSemantic(SemanticAddress.ForConcept(Workspace.IdentityCatalog.Application, "ProjectName"));
    void Because() => _result = Workspace.ProposeRename(Rename<ConceptSyntax>("ProjectName", "ProjectTitle"));

    [Fact] void should_accept() => _result.Accepted.ShouldBeTrue();
    [Fact] void should_repair_every_type_reference() => WorkspaceSyntaxIndex.Create(_result.Workspace).Entries.Count(entry => entry.Node is TypeRefSyntax type && type.Name == "ProjectName").ShouldEqual(0);
    [Fact] void should_retain_identity() => _result.Workspace.IdentityCatalog.ResolveSemantic(SemanticAddress.ForConcept(Workspace.IdentityCatalog.Application, "ProjectTitle")).ShouldEqual(_identity);
    [Fact] void should_propose_two_writes() => _result.WritePlan.Entries.Length.ShouldEqual(2);
    [Fact] void should_leave_the_original_untouched() => Workspace.Documents.Single(document => document.Id == Concepts.Id).Text.ShouldEqual(ConceptsSource);
    [Fact] void should_remain_executable() => _result.ExecutableReady.ShouldBeTrue();
}
