// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_a_provide_takes_its_value_from_code : given.a_semantic_binder
{
    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(
        """
        module Fleet
          feature Quality
            slice StateView Vessels
              provide limit Int
                file Limit.cs
              event VesselRegistered
                vesselId Uuid
        """);

    [Fact] void should_refuse_it_naming_the_issue() => _result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.UnsupportedSemanticSyntax && diagnostic.Message.Contains("(#1300)", StringComparison.Ordinal)).ShouldBeTrue();
}
