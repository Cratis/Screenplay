// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Dependencies.for_DependencyGraph.when_suggesting_story_order;

public class and_siblings_form_a_cycle : given.a_model
{
    DependencyOrder _order;

    void Establish() => _graph = Graph("module M\n  feature A\n    slice StateView V\n      projection R\n        from E\n  feature B\n    slice StateChange W\n      event E\n      command C\n        reads R\n");
    void Because() => _order = _graph.SuggestedOrder();

    [Fact] void should_keep_member_story_order() => _order.Slices.Select(node => node.Address).ShouldEqual(["M.A.V", "M.B.W"]);
    [Fact] void should_leave_authored_container_order_unchanged() => _order.Containers.Any(container => container.Changed).ShouldBeFalse();
}
