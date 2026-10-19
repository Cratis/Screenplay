// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_changing_a_property.with_an_identifier_payload_copy;

public class and_the_revision_is_baselined : given.an_identifier_payload_copy_workspace
{
    ReleaseBaselineRead _baseline = null!;

    void Establish()
    {
        Create(InlineSource);
        var admitted = ReleaseBaseline.Admit(Workspace, ReleaseBaselineStore.ReadBytes(null, Workspace.IdentityCatalog.Application), "v1", DateTimeOffset.UtcNow).Document!;
        _baseline = ReleaseBaselineStore.ReadBytes(ReleaseBaselineStore.Serialize(admitted), Workspace.IdentityCatalog.Application);
    }

    void Because() => Result = Workspace.ProposeProperty(Request() with { ReleaseBaseline = _baseline });

    [Fact] void should_accept_a_new_generation() => Result.Conflicts.ShouldBeEmpty();
    [Fact] void should_report_the_baselined_basis() => Result.PropertyReport!.Generations.Single().Mutability.Basis.ShouldEqual("baselined");
    [Fact] void should_report_a_new_generation() => Result.PropertyReport!.Generations.Single().Mode.ShouldEqual("newGeneration");
    [Fact] void should_keep_the_baselined_generation_intact() => ReleaseBaseline.Check(Result.Workspace!, _baseline).Success.ShouldBeTrue();
    [Fact] void should_still_extract_the_event() => Result.PropertyReport!.ExtractedInlineEvents.Single().Name.ShouldEqual("Renamed");
}
