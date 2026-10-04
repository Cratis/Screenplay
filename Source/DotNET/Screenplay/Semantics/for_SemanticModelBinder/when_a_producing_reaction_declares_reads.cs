// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_a_producing_reaction_declares_reads : given.a_semantic_binder
{
    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(
        """
        module Billing
          feature Decisions
            slice Automation Direct
              readmodel Decision
                id String
              query DecisionById => Decision optional
                by id String
              event Began
              event Decided
              reaction Decide
                when Began
                  reads Decision
                  produces Decided
        """);

    [Fact] void should_refuse_unprotected_direct_production() => _result.Success.ShouldBeFalse();
    [Fact] void should_explain_the_protection_boundary() => _result.Diagnostics.Any(diagnostic => diagnostic.Message.Contains("cannot protect that decision dependency", StringComparison.Ordinal)).ShouldBeTrue();
}
