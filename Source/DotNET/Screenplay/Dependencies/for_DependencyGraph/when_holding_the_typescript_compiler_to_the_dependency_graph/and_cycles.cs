// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Dependencies.for_DependencyGraph.when_holding_the_typescript_compiler_to_the_dependency_graph;

public class and_cycles : given.compiled_vectors
{
    bool _matches;

    void Because() => _matches = _cases.All(item => JsonSerializer.Serialize(item.Graph.Cycles("feature").Select(group => group.Members.Select(node => node.Address).ToArray()).ToArray()) == item.Vector.GetProperty("cycles").GetRawText().Replace(" ", string.Empty));

    [Fact] void should_match_the_shared_vectors() => _matches.ShouldBeTrue();
}
