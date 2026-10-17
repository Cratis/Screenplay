// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_incompatible_production_mappings;

// An optional target accepts an optional source.
public class with_an_optional_target_in_a_reaction : given.a_semantic_binder
{
    const string Source =
        """
        concept Amount : Decimal
        module Beta
          feature Alerts
            slice Automation RaiseAlert
              event Sampled
                amount Amount optional
              event AlertRaised
                amount Amount optional
              reaction RaiseOnSample
                when Sampled
                  amount
                  produces AlertRaised
                    amount = amount
        """;

    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(Source, displayPath: "Beta.play");

    [Fact] void should_bind() => _result.Success.ShouldBeTrue();
    [Fact] void should_report_no_errors() => _result.Diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeFalse();
}
