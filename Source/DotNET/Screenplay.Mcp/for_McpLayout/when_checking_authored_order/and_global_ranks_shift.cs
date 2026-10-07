// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpLayout.when_checking_authored_order;

public class and_global_ranks_shift : given.an_ordered_workspace
{
    void Establish() => After = Workspace(("application.play", Source.Replace(
        "    slice StateView Zulu\n    slice StateView Alpha\n    feature Zulu\n      slice StateView View\n    feature Alpha\n      slice StateView View",
        "    feature Zulu\n      slice StateView View\n    feature Alpha\n      slice StateView View\n    slice StateView Zulu\n    slice StateView Alpha",
        StringComparison.Ordinal)));
    void Because() => KeepsOrder = McpLayout.KeepsAuthoredOrder(Before, After);
    [Fact] void should_compare_relative_order_in_each_collection() => KeepsOrder.ShouldBeTrue();
    [Fact] void should_actually_have_different_global_ranks() => WorkspaceTimelineRepairs.Timeline(Before).Ranks.SequenceEqual(WorkspaceTimelineRepairs.Timeline(After).Ranks).ShouldBeFalse();
}
