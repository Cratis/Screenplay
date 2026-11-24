// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Conformance.for_SemanticModelEquivalence.given;

public class graph_projection_models : compiled_models
{
    protected const string GraphSource = """
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
            slice StateView Details
              readmodel PersonDetails
                name String
        """;
}
