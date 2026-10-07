// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Dependencies.for_DependencyGraph;

public class when_specifications_give_events : given.a_model
{
    void Establish() => _source = "module M\n  feature F\n    slice StateChange Producer\n      event E\n    slice StateView Consumer\n      specification T\n        given E\n";
    void Because() => _graph = Graph(_source);

    [Fact] void should_retain_the_test_only_edge() => _graph.Edges.Single().Kind.ShouldEqual("verifiedWith");
    [Fact] void should_hide_the_edge_by_default() => _graph.Implied("slice", "slice").ShouldBeEmpty();
    [Fact] void should_include_the_edge_on_request() => _graph.Implied("slice", "slice", includeTestOnly: true).Count.ShouldEqual(1);
}
