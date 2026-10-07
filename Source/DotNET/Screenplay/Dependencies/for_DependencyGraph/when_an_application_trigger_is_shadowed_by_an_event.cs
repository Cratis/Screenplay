// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Dependencies.for_DependencyGraph;

public class when_an_application_trigger_is_shadowed_by_an_event : given.a_model
{
    void Establish() => _source = "trigger E\n" + Producer + "  feature B\n    slice Automation Consumer\n      reaction R\n        when E\n";
    void Because() => _graph = Graph(_source);

    [Fact] void should_prefer_the_slice_event() => _graph.Edges.Single().Kind.ShouldEqual("reactsTo");
    [Fact] void should_resolve_the_producing_slice() => _graph.Edges.Single().Producer.Address.ShouldEqual("M.A.Producer");
    [Fact] void should_not_count_the_shadowed_trigger() => _graph.ExcludedReferences.ShouldEqual(0);
}
