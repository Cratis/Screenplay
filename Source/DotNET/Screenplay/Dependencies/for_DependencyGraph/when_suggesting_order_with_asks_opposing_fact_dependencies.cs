// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Dependencies.for_DependencyGraph;

public class when_suggesting_order_with_asks_opposing_fact_dependencies : given.a_model
{
    DependencyOrder _order;

    void Establish() => _graph = Graph("module M\n  feature A\n    slice StateChange First\n      command C\n        produces event E\n      screen S\n        action D\n  feature B\n    slice StateChange Second\n      command D\n      projection P\n        from E\n");
    void Because() => _order = _graph.SuggestedOrder();

    [Fact] void should_exclude_asks_from_ordering() => _order.Containers.Single(order => order.Container.Address == "M").Changed.ShouldBeFalse();
}
