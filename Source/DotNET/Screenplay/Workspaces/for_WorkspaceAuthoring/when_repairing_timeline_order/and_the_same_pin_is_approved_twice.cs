// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring.when_repairing_timeline_order;

public class and_the_same_pin_is_approved_twice : given.a_timeline
{
    [Fact]
    void should_not_consume_one_import_for_two_approved_pins()
    {
        var consumer = Slice("View", [], "E").Replace(" optional", "[]", StringComparison.Ordinal).Replace("    by id Uuid\n", string.Empty, StringComparison.Ordinal);
        var before = Create(
            ("root.play", "module M\n  feature F\n    import \"*.play\"\n"),
            ("a.play", consumer),
            ("z.play", Slice("Write", ["E"])));
        var after = Create(
            ("root.play", "module M\n  feature F\n    import \"z.play\"\n    import \"*.play\"\n"),
            ("a.play", consumer),
            ("z.play", Slice("Write", ["E"])));
        var pin = new TimelineImportPin("root.play", new PlayPlacement(["M", "F"]), new FileImportSyntax("z.play", SourceLocation.Start));
        WorkspaceTimelineContentProof.Preserves(before, after, [pin]).ShouldBeTrue();
        WorkspaceTimelineContentProof.Preserves(before, after, [pin, pin]).ShouldBeFalse();
    }
}
