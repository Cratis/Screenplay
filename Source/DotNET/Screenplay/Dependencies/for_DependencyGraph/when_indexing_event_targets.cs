// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Dependencies.for_DependencyGraph;

public class when_indexing_event_targets : Specification
{
    DependencyGraph _graph = null!;

    void Because() => _graph = DependencyGraph.For(new ScreenplayCompiler().Parse("module M\n  feature F\n    slice StateChange Input\n      event Input\n    slice Translate Publish\n      direction outbound\n      public event E\n      projection P => E\n        from Input\n    slice StateView View\n      readmodel R\n      projection View => R\n        from E\n    slice StateChange Consumer\n      command C\n        reads E\n        reads R\n", "model.play").Value!);

    [Fact] void should_not_offer_an_event_publisher_as_a_readmodel_builder() => _graph.Edges.Where(edge => edge.Kind == "decidesFrom").Select(edge => edge.Producer.Address).ShouldEqual(["M.F.View"]);
    [Fact] void should_report_the_missing_readmodel_at_the_original_read_location() => _graph.Unresolved.Select(reference => $"{reference.Consumer.Address}:{reference.Kind}:{reference.Role}:{reference.Name}:{reference.Location.Path}:{reference.Location.Line}:{reference.Location.Column}").ShouldEqual(["M.F.Consumer:decidesFrom:reads:E:model.play:16:9"]);
    [Fact] void should_keep_the_genuine_input_event_dependency() => _graph.Edges.Where(edge => edge.Kind == "usesFactsFrom" && edge.Consumer.Address == "M.F.Publish").SelectMany(edge => edge.Evidence).Select(evidence => $"{evidence.Producer.Address}:{evidence.Role}:{evidence.Name}:{evidence.Ambiguous}:{evidence.Alternatives.Count}:{evidence.Location.Line}").ShouldEqual(["M.F.Input:from:Input:False:0:9"]);
    [Fact] void should_keep_the_real_view_builder_evidence() => _graph.Edges.Where(edge => edge.Kind == "decidesFrom").SelectMany(edge => edge.Evidence).Where(evidence => evidence.Name == "R").Select(evidence => $"{evidence.Producer.Address}:{evidence.Role}:{evidence.Ambiguous}:{evidence.Alternatives.Count}:{evidence.Location.Line}").ShouldEqual(["M.F.View:reads:False:0:17"]);
}
