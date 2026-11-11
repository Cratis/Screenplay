// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring.when_repairing_timeline_order;

public class and_slices_inside_a_feature_are_swapped : given.a_timeline
{
    static ScreenplayWorkspace Model(bool swapped, string producerType = "Uuid")
    {
        var consumer = Slice("Consumer", [], "E").Replace("ConsumerView optional", "ConsumerView[]", StringComparison.Ordinal).Replace("    by id Uuid\n", string.Empty, StringComparison.Ordinal);
        var producer = Slice("Producer", ["E"]).Replace("id Uuid", $"id {producerType}", StringComparison.Ordinal);
        return Create(("root.play", "module M\n  feature F\n" + Indent(swapped ? producer + consumer : consumer + producer, 4)));
    }

    [Fact]
    void should_accept_a_pure_slice_permutation_in_a_source_only_model()
    {
        var before = Model(false);
        var after = Model(true);
        before.Compilation.Value.ShouldBeNull();
        after.Compilation.Value.ShouldBeNull();
        WorkspaceTimelineContentProof.SameSourceModuloTimelinePermutation(before, after, []).ShouldBeTrue();
        WorkspaceTimelineContentProof.Preserves(before, after, []).ShouldBeTrue();
    }

    [Fact]
    void should_refuse_the_permutation_when_another_edit_is_added()
    {
        var before = Model(false);
        var after = Model(true, "String");
        after.Compilation.Value.ShouldBeNull();
        WorkspaceTimelineContentProof.SameSourceModuloTimelinePermutation(before, after, []).ShouldBeFalse();
        WorkspaceTimelineContentProof.Preserves(before, after, []).ShouldBeFalse();
    }
}
