// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Dependencies.for_DependencyGraph.given;

public class compiled_vectors : a_conformance_suite
{
    private protected (JsonElement Vector, DependencyGraph Graph)[] _cases;

    void Establish() => _cases = [.. _vectors.Select(vector => (vector, Graph(vector)))];
}
