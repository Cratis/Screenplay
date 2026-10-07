// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpLayout.when_checking_authored_order;

public class and_modules_are_swapped : given.an_ordered_workspace
{
    const string Zulu = "module Zulu\n  feature Example\n    slice StateView Example\n";
    const string Alpha = "module Alpha\n  feature Example\n    slice StateView Example\n";

    void Establish()
    {
        Before = Workspace(("application.play", Zulu + Alpha));
        After = Workspace(("application.play", Alpha + Zulu));
    }

    void Because() => KeepsOrder = McpLayout.KeepsAuthoredOrder(Before, After);
    [Fact] void should_refuse_the_changed_module_sequence() => KeepsOrder.ShouldBeFalse();
}
