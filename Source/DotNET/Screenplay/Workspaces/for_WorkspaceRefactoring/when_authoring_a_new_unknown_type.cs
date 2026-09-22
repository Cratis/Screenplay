// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_authoring_a_new_unknown_type : given.a_refactoring_workspace
{
    WorkspaceAuthoringResult _safe = null!;
    WorkspaceAuthoringResult _draft = null!;
    WorkspaceAuthoringRequest _request = null!;

    void Establish() => _request = new()
    {
        ExpectedRevision = Workspace.Revision,
        ExpectedCatalogRevision = Workspace.IdentityCatalog.Revision,
        Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments,
        Validation = WorkspaceAuthoringValidation.Authoring,
        Documents = [new ReplaceWorkspaceSyntaxDocument(Concepts.Id, new ScreenplayCompiler().Parse($"{ConceptsSource}\ntype DraftType\n  value MissingType").Value)]
    };

    void Because()
    {
        _safe = Workspace.ProposeAuthoring(_request);
        _draft = Workspace.ProposeAuthoring(_request with { ReferencePolicy = WorkspaceAuthoringReferencePolicy.Draft });
    }

    [Fact] void should_reject_new_debt_by_default() => _safe.Accepted.ShouldBeFalse();
    [Fact] void should_accept_explicit_draft_debt() => _draft.Accepted.ShouldBeTrue();
    [Fact] void should_disclose_draft_debt() => _draft.AuthoringDiagnostics.Any(diagnostic => diagnostic.Message.Contains("Reference debt", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_never_expose_a_safe_partial_write_plan() => _safe.WritePlan.ShouldBeNull();
}
