// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_a_trivia_patch_would_change_the_wrong_structural_member : given.a_refactoring_workspace
{
    WorkspaceAuthoringResult _result = null!;
    WorkspaceAuthoringRequest _request = null!;

    void Establish()
    {
        var reference = WorkspaceSyntaxIndex.Create(Workspace).Entries.First(entry => entry.Node is TypeRefSyntax type && type.Name == "ProjectName");
        _request = new()
        {
            ExpectedRevision = Workspace.Revision,
            ExpectedCatalogRevision = Workspace.IdentityCatalog.Revision,
            Validation = WorkspaceAuthoringValidation.Authoring,
            Formatting = WorkspaceAuthoringFormatting.PreserveTrivia,
            Operations = [new ReplaceWorkspaceNode(reference.Handle, reference.Node, (TypeRefSyntax)reference.Node with { Name = "ProjectName[]" })]
        };
    }

    void Because() => _result = Workspace.ProposeAuthoring(_request);

    [Fact] void should_reject_a_patch_that_parses_as_a_collection_instead_of_the_intended_name() => _result.Accepted.ShouldBeFalse();
    [Fact] void should_explain_the_ast_proof_failure() => _result.Conflicts.Single().Message.Contains("did not reparse to the intended AST", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_not_expose_any_partial_bytes() => _result.WritePlan.ShouldBeNull();
}
