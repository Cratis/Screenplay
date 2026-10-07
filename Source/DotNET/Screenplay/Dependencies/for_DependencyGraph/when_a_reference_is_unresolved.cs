// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Dependencies.for_DependencyGraph;

public class when_a_reference_is_unresolved : given.a_model
{
    void Establish() => _source = "module M\n  feature F\n    slice StateView Consumer\n      projection P\n        from Missing\n";
    void Because() => _graph = Graph(_source);

    [Fact] void should_retain_the_missing_name() => _graph.Unresolved.Single().Name.ShouldEqual("Missing");
    [Fact] void should_not_invent_an_edge() => _graph.Edges.ShouldBeEmpty();
}
