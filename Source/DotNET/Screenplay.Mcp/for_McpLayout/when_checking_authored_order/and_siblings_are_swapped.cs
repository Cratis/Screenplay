// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpLayout.when_checking_authored_order;

public class and_siblings_are_swapped : given.an_ordered_workspace
{
    void Establish() => After = Workspace(("application.play", Source.Replace("slice StateView Zulu\n    slice StateView Alpha", "slice StateView Alpha\n    slice StateView Zulu", StringComparison.Ordinal)));
    void Because() => KeepsOrder = McpLayout.KeepsAuthoredOrder(Before, After);
    [Fact] void should_refuse_the_changed_sibling_sequence() => KeepsOrder.ShouldBeFalse();
}
