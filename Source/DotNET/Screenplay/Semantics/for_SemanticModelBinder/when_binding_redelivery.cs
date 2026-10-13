// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_redelivery : given.a_semantic_binder
{
    const string Source =
        """
        module Billing
          feature Payments
            slice Automation Recovery
              event Approved
                id String
              reaction Observer
                when Approved
              specification Redelivery
                given Approved
                  id = "other"
                given Approved
                  id = "chosen"
                when redelivered Approved to Observer
                  id = "chosen"
                then no events
        """;

    CompilationResult<SemanticCompilation> _bound;

    void Because() => _bound = Bind(Source);

    [Fact] void should_admit_redelivery() => _bound.Success.ShouldBeTrue();
    [Fact] void should_select_v17() => _bound.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V17);
    [Fact] void should_bind_the_given_occurrence_position_not_the_locator_values() => Scenario.WhenRedelivered!.Given.ShouldEqual(1);
    [Fact] void should_bind_the_named_reaction_identity() => Scenario.WhenRedelivered!.Reaction.ShouldEqual(_bound.Value!.Model.Application.Modules[0].Features[0].Slices[0].Reactions[0].Id);

    [Fact]
    void should_bind_equivalent_locators_to_identical_bytes()
    {
        var qualified = Bind(Source.Replace("to Observer", "to Billing.Payments.Recovery.Observer", StringComparison.Ordinal));
        qualified.Success.ShouldBeTrue();
        SemanticModelSerializer.Serialize(qualified.Value!.Model).ShouldEqual(SemanticModelSerializer.Serialize(_bound.Value!.Model));
    }

    [Theory]
    [InlineData("id = \"missing\"")]
    [InlineData("")]
    void should_refuse_unmatched_or_ambiguous_locators(string locator)
    {
        var source = Source.Replace("when redelivered Approved to Observer\n          id = \"chosen\"", $"when redelivered Approved to Observer\n          {locator}", StringComparison.Ordinal);
        var result = Bind(source);
        result.Success.ShouldBeFalse();
        result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.UnmatchedRedeliveredOccurrence).ShouldBeTrue();
    }

    SemanticSpecification Scenario => _bound.Value!.Model.Application.Modules[0].Features[0].Slices[0].Specifications[0];
}
