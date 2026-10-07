// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Dependencies.for_DependencyGraph;

public class when_specifications_expect_events : given.a_model
{
    void Establish() => _source = Producer + "  feature B\n    slice StateView Consumer\n      specification T\n        when C\n        then E\n";
    void Because() => _graph = Graph(_source);

    [Fact] void should_resolve_the_declaring_slice() => _graph.Edges.Single(edge => edge.Kind == "verifiedWith").Producer.Address.ShouldEqual("M.A.Producer");
    [Fact] void should_retain_the_reference_role() => _graph.Edges.Single(edge => edge.Kind == "verifiedWith").Evidence.Any(item => item.Role == "thenEvent").ShouldBeTrue();
}
