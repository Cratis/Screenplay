// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.given;

public class a_graph_projection_source : a_semantic_binder
{
    protected const string Source = """
        command reads protected
        module Organization
          feature Chart
            slice StateView Overview
              event PersonAdded
              graph People
                node Person
                  personId Uuid key
              projection BuildPeople => graph People
                node Person
                  from PersonAdded key $eventSourceId
                    personId = $eventSourceId
        """;

    protected const string Projection = """
              projection BuildPeople => graph People
                node Person
                  from PersonAdded key $eventSourceId
                    personId = $eventSourceId
        """;

    protected ExecutableSemanticModel Compile(string source)
    {
        var result = Bind(source);
        if (!result.Success) throw new InvalidSemanticContract(string.Join('\n', result.Diagnostics.Select(diagnostic => diagnostic.Message)));
        return result.Value!.Model;
    }
}
