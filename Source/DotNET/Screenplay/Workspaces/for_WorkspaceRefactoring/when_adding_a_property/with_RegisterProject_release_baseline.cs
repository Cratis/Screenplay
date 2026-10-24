// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_adding_a_property;

public class with_RegisterProject_release_baseline : given.a_RegisterProject_property_workspace
{
    ReleaseBaselineRead _baseline = null!;

    void Because()
    {
        var admitted = ReleaseBaseline.Admit(Workspace, ReleaseBaselineStore.ReadBytes(null, Workspace.IdentityCatalog.Application), "v1", DateTimeOffset.UtcNow).Document!;
        _baseline = Read(admitted with { Entries = [.. admitted.Entries.Where(entry => entry.Revision == 1)] });
        Result = Workspace.ProposeProperty(CorpusRequest() with { ReleaseBaseline = _baseline });
    }

    [Fact] void should_bind_generation_two_after_an_in_place_edit() => Result.ExecutableReady.ShouldBeTrue();
    [Fact] void should_pass_every_model_specification() => Runs().SelectMany(run => run.Failures).ShouldBeEmpty();
    [Fact] void should_preserve_frozen_generation_one() => ReleaseBaseline.Check(Result.Workspace!, _baseline).Success.ShouldBeTrue();
    [Fact] void should_report_an_unadmitted_in_place_edit() => Result.PropertyReport!.Generations.Single().Mode.ShouldEqual("inPlace");
    [Fact] void should_report_the_mutability_basis() => Result.PropertyReport!.Generations.Single().Mutability.Basis.ShouldEqual("notInBaseline");
    [Fact] void should_not_add_generation_three() => Events.Length.ShouldEqual(2);

    [Fact]
    void should_refuse_in_place_edits_after_generation_two_is_admitted()
    {
        var workspace = Result.Workspace!;
        var admitted = ReleaseBaseline.Admit(workspace, _baseline, "v2", DateTimeOffset.UtcNow).Document!;
        var owner = WorkspaceSyntaxIndex.Create(workspace).Entries.Single(entry =>
            (entry.Node is CommandSyntax or EventSyntax or ReadModelSyntax) &&
            WorkspaceReferenceBindings.Name(entry.Node) == "ProjectRegistered" &&
            entry.Node is EventSyntax @event && @event.Generation == 2);
        var refused = workspace.ProposeProperty(new WorkspacePropertyRequest
        {
            ExpectedRevision = workspace.Revision,
            ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
            Action = "add",
            Owner = owner.Handle,
            ExpectedOwnerName = "ProjectRegistered",
            Name = "another",
            Type = "String",
            Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments,
            ReleaseBaseline = ReleaseBaselineStore.ReadBytes(ReleaseBaselineStore.Serialize(admitted), workspace.IdentityCatalog.Application)
        });
        refused.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.BaselinedRevisionImmutable);
        refused.Workspace.ShouldBeNull();
    }

    ReleaseBaselineRead Read(ReleaseBaselineDocument document) => ReleaseBaselineStore.ReadBytes(ReleaseBaselineStore.Serialize(document), Workspace.IdentityCatalog.Application);
}
