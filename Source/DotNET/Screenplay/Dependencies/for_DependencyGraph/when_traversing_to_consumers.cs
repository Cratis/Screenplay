// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Dependencies.for_DependencyGraph;

public class when_traversing_to_consumers : given.a_model
{
    IReadOnlyList<DependencyNode> _nodes;

    void Establish() => _graph = Graph("module M\n  feature F\n    slice StateView V\n      projection P\n        from E\n    slice StateChange W\n      event E\n    slice StateView Last\n      command C\n        reads P\n");
    void Because() => _nodes = _graph.Traverse("M.F.W", "incoming");

    [Fact] void should_reach_each_consumer_once() => _nodes.Select(node => node.Address).ShouldEqual(["M.F.V", "M.F.Last"]);
}
