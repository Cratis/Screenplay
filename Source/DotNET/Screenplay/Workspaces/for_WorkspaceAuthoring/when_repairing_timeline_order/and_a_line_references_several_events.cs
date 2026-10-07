// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring.when_repairing_timeline_order;

public class and_a_line_references_several_events : given.a_timeline
{
    [Fact]
    void should_identify_the_reference_node_by_event_name_not_just_location()
    {
        var source = "module M\n  feature F\n" + Indent(Slice("Consumer", [], "E", "X"), 4) + Indent(Slice("Producer", ["E", "X"]), 4);
        source = source.Replace("from E key id\n        from X key id", "from E, X key id", StringComparison.Ordinal);
        var workspace = Create(("root.play", source));
        foreach (var name in new[] { "E", "X" })
        {
            var repair = Repair(workspace, name);
            ((EventSpecSyntax)WorkspaceSyntaxIndex.Create(workspace).Find(repair.Subject)!.Node).Event.ShouldEqual(name);
            Preserves(workspace, repair);
        }
    }
}
