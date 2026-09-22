// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring;

public class when_bootstrapping_an_empty_workspace : given.an_authoring_workspace
{
    ScreenplayWorkspace _empty = null!;
    WorkspaceAuthoringResult _result = null!;

    void Establish() => _empty = ScreenplayWorkspace.CreateEmpty(ApplicationIdentity.Create("Projects"), "Projects");

    void Because() => _result = _empty.ProposeAuthoring(new WorkspaceAuthoringRequest
    {
        ExpectedRevision = _empty.Revision,
        ExpectedCatalogRevision = _empty.IdentityCatalog.Revision,
        Validation = WorkspaceAuthoringValidation.Executable,
        Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments,
        Documents =
        [
            new CreateWorkspaceSyntaxDocument("concepts", Concepts.Path, Syntax(ConceptsSource)),
            new CreateWorkspaceSyntaxDocument("registration", Registration.Path, Syntax(RegistrationSource))
        ]
    });

    [Fact] void should_create_both_documents_atomically() => _result.Workspace.Documents.Length.ShouldEqual(2);
    [Fact] void should_accept_a_coherent_final_application() => _result.Accepted.ShouldBeTrue();
    [Fact] void should_be_executable() => _result.ExecutableReady.ShouldBeTrue();
    [Fact] void should_plan_two_new_documents() => _result.WritePlan.Entries.Count(entry => entry.Kind == WorkspaceWriteKind.Added).ShouldEqual(2);
    [Fact] void should_keep_the_empty_base_unchanged() => _empty.Documents.ShouldBeEmpty();
    [Fact] void should_round_trip_the_empty_base() => ScreenplayWorkspaceSerializer.Deserialize(ScreenplayWorkspaceSerializer.Serialize(_empty)).Revision.ShouldEqual(_empty.Revision);
}
