// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Indexing;
using Cratis.Screenplay.Indexing.for_AuthoringIndex.given;

namespace Cratis.Screenplay.Dependencies.for_DependencyGraph;

public class when_preserving_event_target_boundaries : Specification
{
    (string Name, DependencyGraph Graph, string Target, bool Builder)[] _cases = [];

    void Because() => _cases = [.. event_target_applications.Boundaries().Select(value =>
    {
        var sources = new Dictionary<string, string>(value.Sources)
        {
            ["zz-consumer.play"] = "module M\n  feature F\n    slice StateChange Consumer\n      command C\n        reads " + (value.Target.Contains('.', StringComparison.Ordinal) ? "E" : value.Target)
        };
        return (value.Name, new AuthoringSnapshot(sources).DependencyGraph, value.Target.Contains('.', StringComparison.Ordinal) ? "E" : value.Target, value.Builder);
    })];

    [Fact]
    void should_keep_builder_precedence_and_all_evidence()
    {
        foreach (var value in _cases)
        {
            value.Graph.Edges.Where(edge => edge.Kind == "decidesFrom").SelectMany(edge => edge.Evidence).Select(evidence => $"{evidence.Consumer.Address}:{evidence.Producer.Address}:{evidence.Role}:{evidence.Name}:{evidence.Ambiguous}:{evidence.Alternatives.Count}:{evidence.Location.Path}:{evidence.Location.Line}:{evidence.Location.Column}").ShouldEqual(value.Builder ? ["M.F.Consumer:M.F.Publish:reads:" + value.Target + ":False:0:zz-consumer.play:5:9"] : []);
        }
    }

    [Fact]
    void should_keep_unresolved_reads_without_a_matching_builder()
    {
        foreach (var value in _cases)
        {
            value.Graph.Unresolved.Where(reference => reference.Role == "reads").Select(reference => reference.Name).ShouldEqual(value.Builder ? [] : [value.Target]);
        }
    }
}
