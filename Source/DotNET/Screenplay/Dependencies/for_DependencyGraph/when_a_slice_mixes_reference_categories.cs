// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Dependencies.for_DependencyGraph;

public class when_a_slice_mixes_reference_categories : given.a_model
{
    void Establish() => _source = "trigger Tick\nimport Outside.Imported\nmodule M\n  feature F\n    slice StateChange Producer\n      event E\n    slice StateView Consumer\n      event Local\n      projection P\n        from Local\n        from Missing\n        from Imported\n      reaction R\n        when Tick\n      specification T\n        given E\n";
    void Because() => _graph = Graph(_source);

    [Fact] void should_keep_only_external_and_test_edges() => _graph.Edges.Count.ShouldEqual(2);
    [Fact] void should_resolve_the_outside_context() => _graph.Edges.Single(edge => edge.Kind == "outsideTheModel").Producer.Address.ShouldEqual("context:Outside");
    [Fact] void should_exclude_test_edges_by_default() => _graph.Implied("slice", "slice").Count.ShouldEqual(0);
    [Fact] void should_include_test_edges_on_request() => _graph.Implied("slice", "slice", includeTestOnly: true).Count.ShouldEqual(1);
    [Fact] void should_retain_the_unresolved_reference() => _graph.Unresolved.Single().Name.ShouldEqual("Missing");
    [Fact] void should_count_the_shared_trigger() => _graph.ExcludedReferences.ShouldEqual(1);
}
