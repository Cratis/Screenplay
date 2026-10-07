// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpLayout.when_checking_authored_order;

public class and_only_paths_change : given.an_ordered_workspace
{
    void Establish() => After = Workspace(("Story.play", Source));
    void Because() => KeepsOrder = McpLayout.KeepsAuthoredOrder(Before, After);
    [Fact] void should_keep_the_same_relative_sibling_order() => KeepsOrder.ShouldBeTrue();
}
