// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring.when_repairing_timeline_order;

public class and_the_reference_is_a_trigger_or_projection_entry : given.a_timeline
{
    [Theory]
    [InlineData("slice Automation Consumer\n  reaction Observe\n    when E\n      produces Seen\n  event Seen\n", typeof(NamedTriggerSourceSyntax))]
    [InlineData("slice StateView Consumer\n  readmodel View\n    id String\n  projection Build\n    variant View\n      enters on E key id\n", typeof(ProjectionEntersOnSyntax))]
    void should_repair_the_typed_event_reference(string consumer, Type subjectType)
    {
        var workspace = Create(("root.play", "module M\n  feature F\n" + Indent(consumer, 4) + Indent(Slice("Producer", ["E"]), 4)));
        WorkspaceTimelineRepairs.Timeline(workspace).SourceValid.ShouldBeTrue();
        var finding = Finding(workspace, "E");
        var repair = Repair(workspace, "E");
        var subject = WorkspaceSyntaxIndex.Create(workspace).Find(repair.Subject)!;
        subject.Node.GetType().ShouldEqual(subjectType);
        subject.Location.ShouldEqual(finding.Location);
        Propose(workspace, repair).Workspace!.Compilation.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.EventFromLaterSlice).ShouldBeEmpty();
    }
}
