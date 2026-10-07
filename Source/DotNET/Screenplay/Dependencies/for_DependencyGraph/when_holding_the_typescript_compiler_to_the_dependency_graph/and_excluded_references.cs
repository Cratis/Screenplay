// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Dependencies.for_DependencyGraph.when_holding_the_typescript_compiler_to_the_dependency_graph;

public class and_excluded_references : given.compiled_vectors
{
    bool _matches;

    void Because() => _matches = _cases.All(item => item.Graph.ExcludedReferences == (item.Vector.TryGetProperty("excludedReferences", out var excluded) ? excluded.GetInt32() : 0));

    [Fact] void should_match_the_shared_vectors() => _matches.ShouldBeTrue();
}
