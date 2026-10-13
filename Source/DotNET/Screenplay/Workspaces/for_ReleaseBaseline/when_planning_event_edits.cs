// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_ReleaseBaseline;

public class when_planning_event_edits : given.a_baseline_workspace
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_require_only_new_generation_for_a_baselined_revision(bool neverPersisted)
    {
        var result = Workspace.ProposeProperty(Add(Read(Baseline)) with { EventNeverPersisted = neverPersisted });
        result.Accepted.ShouldBeFalse();
        result.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.BaselinedRevisionImmutable);
        result.Conflicts.Single().Message.ShouldContain("newGeneration");
        result.Workspace.ShouldBeNull();
    }

    [Fact]
    void should_honor_an_explicit_new_generation_and_report_the_release()
    {
        var result = Workspace.ProposeProperty(Add(Read(Baseline)) with { NewGeneration = true });
        Assert.True(result.Accepted, string.Join('\n', result.Conflicts.Select(conflict => conflict.Message)));
        var generation = result.PropertyReport!.Generations.Single();
        generation.From.ShouldEqual(2U);
        generation.To.ShouldEqual(3U);
        generation.Mode.ShouldEqual("newGeneration");
        generation.Mutability.ShouldEqual(new("baselined", Read(Baseline).Revision, "v1.0.0"));
        ReleaseBaseline.Check(result.Workspace!, Read(Baseline)).Success.ShouldBeTrue();
    }

    [Theory]
    [InlineData(false, false, "inPlace", 2U)]
    [InlineData(true, false, "eventNeverPersisted", 2U)]
    [InlineData(false, true, "newGeneration", 3U)]
    void should_edit_an_unadmitted_current_revision_without_an_assertion(bool neverPersisted, bool advance, string mode, uint to)
    {
        var baseline = Read(Baseline with { Entries = [Baseline.Entries[0]] });
        var result = Workspace.ProposeProperty(Add(baseline) with { EventNeverPersisted = neverPersisted, NewGeneration = advance });
        Assert.True(result.Accepted, string.Join('\n', result.Conflicts.Select(conflict => conflict.Message)));
        var generation = result.PropertyReport!.Generations.Single();
        generation.Mode.ShouldEqual(mode);
        generation.To.ShouldEqual(to);
        generation.Mutability.ShouldEqual(new("notInBaseline", baseline.Revision, null));
        ReleaseBaseline.Check(result.Workspace!, baseline).Success.ShouldBeTrue();
    }

    [Fact]
    void should_preserve_the_no_baseline_choice()
    {
        Workspace.ProposeProperty(Add(Absent)).Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.EventContractChangeRequiresDecision);
        var result = Workspace.ProposeProperty(Add(Absent) with { EventNeverPersisted = true });
        result.PropertyReport!.Generations.Single().Mutability.ShouldEqual(new("noBaseline", null, null));
    }

    [Fact]
    void should_refuse_unreadable_event_mutability()
    {
        var result = Workspace.ProposeProperty(Add(new("unreadable", null, null, "conflict")));
        result.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.UnreadableReleaseBaseline);
    }

    [Theory]
    [InlineData("absent", true)]
    [InlineData("empty", false)]
    [InlineData("admitted", true)]
    void should_report_whether_an_event_rename_added_a_pin(string basis, bool pin)
    {
        var baseline = basis switch { "absent" => Absent, "empty" => Read(Baseline with { Entries = [] }), _ => Read(Baseline) };
        var result = Workspace.ProposeRename(Rename(baseline));
        Assert.True(result.Accepted, string.Join('\n', result.Conflicts.Select(conflict => conflict.Message)));
        result.EventRenameReport!.PinAdded.ShouldEqual(pin);
        result.EventRenameReport.Mutability.Basis.ShouldEqual(basis switch { "absent" => "noBaseline", "empty" => "notInBaseline", _ => "baselined" });
    }

    [Fact]
    void should_refuse_the_never_persisted_rename_assertion_for_any_admitted_revision()
    {
        var result = Workspace.ProposeRename(Rename(Read(Baseline with { Entries = [Baseline.Entries[0]] })) with { EventNeverPersisted = true });
        result.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.BaselinedRevisionImmutable);
    }

    WorkspacePropertyRequest Add(ReleaseBaselineRead baseline) => new()
    {
        ExpectedRevision = Workspace.Revision,
        ExpectedCatalogRevision = Workspace.IdentityCatalog.Revision,
        Action = "add",
        Owner = CurrentEvent().Handle,
        ExpectedOwnerName = "Recorded",
        Name = "priority",
        Type = "String",
        Optional = true,
        ReleaseBaseline = baseline,
        Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments
    };

    WorkspaceRenameRequest Rename(ReleaseBaselineRead baseline) => new()
    {
        ExpectedRevision = Workspace.Revision,
        ExpectedCatalogRevision = Workspace.IdentityCatalog.Revision,
        Target = CurrentEvent().Handle,
        ExpectedName = "Recorded",
        NewName = "Captured",
        ReleaseBaseline = baseline,
        Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments
    };

    WorkspaceSyntaxEntry CurrentEvent() => WorkspaceSyntaxIndex.Create(Workspace).Entries.Single(entry => entry.Node is EventSyntax { Generation: 2 });
}
