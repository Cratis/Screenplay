// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Dependencies.for_DependencyGraph;

public class when_counting_and_ordering_evidence : given.a_model
{
    ImpliedDependency _edge;

    void Establish() => _graph = Graph("module M\n  feature F\n    slice StateView V\n      projection P\n        from E\n        from E\n      command C\n        reads R\n    feature Sub\n      slice StateChange W\n        event E\n        projection R\n          from E\nmodule N\n  feature G\n    slice StateView Other\n      projection P\n        from E\n");
    void Because() => _edge = _graph.Implied("slice", "slice", evidenceLimit: 1)[0];

    [Fact] void should_count_one_slice_pair() => _edge.SliceEdges.ShouldEqual(1);
    [Fact] void should_count_every_reference() => _edge.References.ShouldEqual(3);
    [Fact] void should_retain_the_uncapped_evidence_count() => _edge.EvidenceCount.ShouldEqual(3);
    [Fact] void should_report_truncated_evidence() => _edge.EvidenceTruncated.ShouldBeTrue();
    [Fact] void should_count_facts_by_kind() => _edge.ByKind["usesFactsFrom"].ShouldEqual(2);
    [Fact] void should_count_decisions_by_kind() => _edge.ByKind["decidesFrom"].ShouldEqual(1);
    [Fact] void should_count_distinct_consumers() => _edge.Consumers.Count.ShouldEqual(1);
    [Fact] void should_count_distinct_producers() => _edge.Producers.Count.ShouldEqual(1);
}
