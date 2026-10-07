// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Dependencies.for_DependencyGraph.when_holding_the_typescript_compiler_to_the_dependency_graph;

public class and_unresolved_references : given.compiled_vectors
{
    bool _matches;

    void Because() => _matches = _cases.All(item => item.Graph.Unresolved.Select(reference => $"{reference.Consumer.Address}|{reference.Kind}|{reference.Role}:{reference.Name}").SequenceEqual(item.Vector.TryGetProperty("unresolved", out var unresolved) ? unresolved.EnumerateArray().Select(reference => reference.GetString()) : []));

    [Fact] void should_match_the_shared_vectors() => _matches.ShouldBeTrue();
}
