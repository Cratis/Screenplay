// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.when_running_reactions;

public class and_a_capture_key_is_numeric_instead_of_text : given.a_v6_scenario
{
    const string Reason = "Capture 'RegisterFeed' key 'PlantId' is 1201 (number); event 'PlantListed' has a text event source, so the key must be text: write \"1201\".";
    SemanticSpecificationRun _numeric;
    SemanticSpecificationRun _text;

    void Establish() => Compile(
        """
        domain Acme
        concept PlantNumber : Int
        concept PlantName : String
        module Plants
          feature Register
            slice Translate MirrorRegister
              capture RegisterFeed
                source api
                  api Registry
                  route /api/v1/plants
                  poll 1d
                key PlantId
                append PlantListed
                  when added
                    name = $.PlantName
              event PlantListed
                name PlantName
              specification NumericKey
                when capture RegisterFeed
                  PlantId = 1201
                  PlantName = "Falls"
                then PlantListed
                  for "1201"
                  name = "Falls"
              specification TextKey
                when capture RegisterFeed
                  PlantId = "1201"
                  PlantName = "Falls"
                then PlantListed
                  for "1201"
                  name = "Falls"
        """);

    void Because()
    {
        _numeric = Run("NumericKey");
        _text = Run("TextKey");
    }

    [Fact] void should_fail_the_numeric_key_specification() => _numeric.Passed.ShouldBeFalse();
    [Fact] void should_reject_the_numeric_key_as_a_contract_violation() => ((SemanticRejected)_numeric.Execution).Category.ShouldEqual(SemanticRejectionCategory.Contract);
    [Fact] void should_explain_the_key_value_type_and_correction_in_the_rejection() => ((SemanticRejected)_numeric.Execution).Details.ShouldEqual(Reason);
    [Fact] void should_preserve_the_reason_in_the_reported_failure() => _numeric.Failures.ShouldEqual(new[] { $"Expected Accepted, got Rejected. {Reason}" });
    [Fact] void should_append_nothing_for_the_numeric_key() => _numeric.Execution.World.Facts.ShouldBeEmpty();
    [Fact] void should_still_pass_the_text_key_control() => _text.Passed.ShouldBeTrue();
}
