// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_incompatible_production_mappings;

// An optional trigger property mapped into a required produced property is refused at the mapping, not at the application root (#785).
public class with_an_optional_trigger_property_in_a_reaction : given.a_semantic_binder
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
                amount Amount
              reaction RaiseOnSample
                when Sampled
                  amount
                  produces AlertRaised
                    amount = amount
        """;

    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(Source, displayPath: "Beta.play");

    [Fact] void should_fail_the_bind() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_one_binding_diagnostic() => _result.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidSemanticBinding).ShouldEqual(1);
    [Fact] void should_name_the_property_and_both_types() => Reported.Message.ShouldEqual("Mapping 'amount' into event 'AlertRaised' assigns an optional 'Amount' to the required property 'amount'.");
    [Fact] void should_locate_the_mapping() => (Reported.Location.Line, Reported.Location.Column).ShouldEqual((13, 13));
    [Fact] void should_not_report_the_application_root() => Reported.Location.Line.ShouldNotEqual(1);

    Diagnostic Reported => _result.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidSemanticBinding);
}
