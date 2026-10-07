// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Dependencies.for_DependencyGraph;

public class when_a_slice_uses_shared_types_and_policies : given.a_model
{
    void Establish() => _source = "concept Id : Uuid\npolicy Allowed\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        id Id\n        authorize Allowed\n";
    void Because() => _graph = Graph(_source);

    [Fact] void should_exclude_shared_foundation_edges() => _graph.Edges.Count.ShouldEqual(0);
    [Fact] void should_count_both_shared_references() => _graph.ExcludedReferences.ShouldEqual(2);
}
