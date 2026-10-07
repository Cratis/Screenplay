// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring.when_repairing_timeline_order;

public class and_there_is_no_ordering_root : given.a_timeline
{
    [Fact]
    void should_have_no_backward_timeline_findings_to_repair()
    {
        var workspace = Create(
            ("a.play", "module M\n  feature F\n" + Indent(Slice("Consumer", [], "E"), 4)),
            ("z.play", "module M\n  feature F\n" + Indent(Slice("Producer", ["E"]), 4)));
        workspace.Compilation.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.EventFromLaterSlice).ShouldBeEmpty();
    }
}
