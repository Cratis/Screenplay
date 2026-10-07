// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpLayout.when_checking_authored_order;

public class and_features_are_swapped : given.an_ordered_workspace
{
    const string Zulu = "  feature Zulu\n    slice StateView Example\n";
    const string Alpha = "  feature Alpha\n    slice StateView Example\n";

    void Establish()
    {
        Before = Workspace(("application.play", "module Example\n" + Zulu + Alpha));
        After = Workspace(("application.play", "module Example\n" + Alpha + Zulu));
    }

    void Because() => KeepsOrder = McpLayout.KeepsAuthoredOrder(Before, After);
    [Fact] void should_refuse_the_changed_feature_sequence() => KeepsOrder.ShouldBeFalse();
}
