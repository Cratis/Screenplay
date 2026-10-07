// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Dependencies.for_DependencyGraph;

public class when_specifications_redeliver_events : given.a_model
{
    void Establish() => _source = Producer + "  feature B\n    slice Automation Observer\n      reaction R\n        when E\n    slice Automation Consumer\n      specification T\n        given E\n        when redelivered E to R\n";
    void Because() => _graph = Graph(_source);

    [Fact] void should_retain_the_event_reference_role() => _graph.Edges.SelectMany(edge => edge.Evidence).Any(item => item.Role == "whenRedeliveredEvent").ShouldBeTrue();
    [Fact] void should_retain_the_reaction_reference_role() => _graph.Edges.SelectMany(edge => edge.Evidence).Any(item => item.Role == "redeliveryReaction").ShouldBeTrue();
    [Fact] void should_resolve_the_observing_slice() => _graph.Edges.Single(edge => edge.Kind == "verifiedWith" && edge.Producer.Address == "M.B.Observer").Evidence.Single().Role.ShouldEqual("redeliveryReaction");
}
