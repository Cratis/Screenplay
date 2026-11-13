// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_a_gate_names_an_unknown_availability : given.a_semantic_binder
{
    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(
        """
        module Fleet
          feature Quality
            available with Nowhere
            slice StateView Vessels
              event VesselRegistered
                vesselId Uuid
        """);

    [Fact] void should_not_open_the_gate_by_dropping_it() => _result.Success.ShouldBeFalse();
}
