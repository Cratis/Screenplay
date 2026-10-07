// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Dependencies.for_DependencyGraph.when_suggesting_story_order;

public class and_producers_follow_consumers : given.a_model
{
    DependencyOrder _order;

    void Establish() => _graph = Graph("module M\n  feature F\n    slice StateView V\n      projection P\n        from E\n    slice StateChange W\n      event E\n    slice StateView Last\n      command C\n        reads P\n");
    void Because() => _order = _graph.SuggestedOrder();

    [Fact] void should_put_producers_first() => _order.Slices.Select(node => node.Address).ShouldEqual(["M.F.W", "M.F.V", "M.F.Last"]);
    [Fact] void should_report_the_changed_container() => _order.Containers.Single(container => container.Container.Address == "M.F").Changed.ShouldBeTrue();
}
