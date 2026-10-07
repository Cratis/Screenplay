// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpLayout.when_checking_authored_order;

public class and_containers_are_split_across_documents : given.an_ordered_workspace
{
    void Establish()
    {
        Before = Workspace(
            ("application.play", "import \"First.play\"\nimport \"Second.play\"\n"),
            ("First.play", "module Zulu\n  feature View\n    slice StateView Zulu\nmodule Alpha\n  feature View\n    slice StateView View\n"),
            ("Second.play", "module Zulu\n  feature View\n    slice StateView Alpha\n"));
        After = Workspace(
            ("application.play", "import \"Zulu.play\"\nimport \"Alpha.play\"\n"),
            ("Zulu.play", "module Zulu\n  feature View\n    slice StateView Zulu\n    slice StateView Alpha\n"),
            ("Alpha.play", "module Alpha\n  feature View\n    slice StateView View\n"));
    }

    void Because() => KeepsOrder = McpLayout.KeepsAuthoredOrder(Before, After);
    [Fact] void should_compare_each_logical_sibling_at_its_first_occurrence() => KeepsOrder.ShouldBeTrue();
}
