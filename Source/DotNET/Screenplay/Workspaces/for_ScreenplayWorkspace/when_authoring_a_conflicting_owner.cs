// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspace;

public class when_authoring_a_conflicting_owner : given.a_valid_workspace
{
    WorkspaceAuthoringResult _result = null!;

    void Because() => _result = Workspace.ProposeAuthoring(new()
    {
        ExpectedRevision = Workspace.Revision,
        ExpectedCatalogRevision = Workspace.IdentityCatalog.Revision,
        Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments,
        Validation = WorkspaceAuthoringValidation.Authoring,
        Documents = [new CreateWorkspaceSyntaxDocument(
            "duplicate-registration",
            PortablePlayPath.Parse("DuplicateProjects.play"),
            new ScreenplayCompiler().Parse("module Projects\n  feature Registration\n    slice StateChange RegisterProject").Value!)]
    });

    [Fact] void should_reject_the_transaction() => _result.Accepted.ShouldBeFalse();
    [Fact] void should_report_the_typed_conflict() => _result.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.ConflictingOwner);
    [Fact] void should_identify_both_owners() => new[] { _result.Conflicts.Single().Path!.Value, _result.Conflicts.Single().OtherPath!.Value }.Order(StringComparer.Ordinal).SequenceEqual(new[] { "DuplicateProjects.play", Registration.Path.Value }.Order(StringComparer.Ordinal)).ShouldBeTrue();
    [Fact] void should_offer_no_write_plan() => _result.WritePlan.ShouldBeNull();
}
