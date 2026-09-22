// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_draft_would_lose_an_existing_reference : given.a_refactoring_workspace
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
            Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments,
            Validation = WorkspaceAuthoringValidation.Authoring,
            ReferencePolicy = WorkspaceAuthoringReferencePolicy.Draft,
            Operations = [new ReplaceWorkspaceNode(reference.Handle, reference.Node, (TypeRefSyntax)reference.Node with { Name = "MissingType" })]
        };
    }

    void Because() => _result = Workspace.ProposeAuthoring(_request);

    [Fact] void should_reject_the_lost_binding_even_in_draft() => _result.Accepted.ShouldBeFalse();
    [Fact] void should_not_expose_partial_writes() => _result.WritePlan.ShouldBeNull();
}
