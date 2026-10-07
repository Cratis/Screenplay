// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Dependencies.for_DependencyGraph.when_holding_the_typescript_compiler_to_the_dependency_graph;

public class and_slice_edges : given.compiled_vectors
{
    bool _matches;

    void Because() => _matches = _cases.All(item => item.Graph.Edges.Select(edge => $"{edge.Consumer.Address}|{edge.Producer.Address}|{edge.Kind}|{string.Join(',', edge.Evidence.Select(evidence => evidence.Role + ":" + evidence.Name))}").SequenceEqual(item.Vector.GetProperty("edges").EnumerateArray().Select(edge => edge.GetString())));

    [Fact] void should_match_the_shared_vectors() => _matches.ShouldBeTrue();
}
