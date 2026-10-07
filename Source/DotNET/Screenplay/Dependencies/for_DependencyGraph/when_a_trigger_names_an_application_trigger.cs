// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Dependencies.for_DependencyGraph;

public class when_a_trigger_names_an_application_trigger : given.a_model
{
    void Establish() => _source = "trigger Tick\nmodule M\n  feature F\n    slice Automation Consumer\n      reaction R\n        when Tick\n";
    void Because() => _graph = Graph(_source);

    [Fact] void should_exclude_the_shared_trigger() => _graph.Edges.ShouldBeEmpty();
    [Fact] void should_count_the_shared_reference() => _graph.ExcludedReferences.ShouldEqual(1);
}
