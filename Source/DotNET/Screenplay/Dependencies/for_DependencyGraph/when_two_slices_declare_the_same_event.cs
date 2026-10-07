// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Dependencies.for_DependencyGraph;

public class when_two_slices_declare_the_same_event : given.a_model
{
    void Establish() => _source = "module M\n  feature F\n    slice StateChange First\n      event E\n      event E generation 2\n    slice StateChange Second\n      event e\n    slice StateView V\n      projection P\n        from E\n";
    void Because() => _graph = Graph(_source);

    [Fact] void should_select_the_earliest_producer() => _graph.Edges.Single().Evidence.Single().Producer.Address.ShouldEqual("M.F.First");
    [Fact] void should_report_ambiguity() => _graph.Edges.Single().Evidence.Single().Ambiguous.ShouldBeTrue();
    [Fact] void should_list_the_other_slice_without_counting_generations_twice() => _graph.Edges.Single().Evidence.Single().Alternatives.Select(node => node.Address).ShouldEqual(["M.F.Second"]);
}
