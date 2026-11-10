// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Comparison.for_ModelComparison;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_renaming_a_row_narrowing_policy : given.a_refactoring_workspace
{
    WorkspaceAuthoringResult _result = null!;

    void Establish() => Workspace = ScreenplayWorkspace.Create("Invoices", [Document("invoices", "application.play", when_comparing_agent_exposure.Narrowed)], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Invoices")));

    void Because() => _result = Workspace.ProposeRename(Rename<PolicySyntax>("VisibleRows", "AllowedRows"));

    [Fact] void should_preserve_the_existing_refusal_to_rename_policies() => _result.Accepted.ShouldBeFalse();
    [Fact] void should_not_change_the_source() => Workspace.Documents.Single().Text.ShouldEqual(when_comparing_agent_exposure.Narrowed);
    [Fact] void should_resolve_the_narrowing_reference_for_reference_consumers() => new WorkspaceReferenceBindings(WorkspaceSyntaxIndex.Create(Workspace)).Bindings.Single(binding => binding.Reference.Domain == WorkspaceReferenceDomain.Policy && binding.Reference.Text == "VisibleRows").Target.ShouldNotBeNull();
}
