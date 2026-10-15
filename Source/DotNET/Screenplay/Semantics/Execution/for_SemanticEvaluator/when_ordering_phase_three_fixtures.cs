// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.given;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator;

public class when_ordering_phase_three_fixtures : a_v6_scenario
{
    const string Source = "concept Code : String\n  generate pattern \"?{2}\"\nconcept Ticket : Int\n  generate random 1..9\npolicy SignedIn\n  require authenticated\nmodule M\n  feature F\n    slice StateChange S\n      command Create\n        first Code generated\n        later Ticket generated\n        name String\n        validate\n          name min 1\n        authorize SignedIn\n        returns later";

    void Establish() => Compile(Source);

    SemanticCommand Command => _plan.Commands.Values.Single();
    SemanticPropertyValue Value(string name, SemanticValue value) => new(Command.Properties.Single(property => property.Name == name).Id, value);
    SemanticExecutionRequest Request => SemanticExecutionRequest.Create(Command.Id, [Value("name", SemanticValue.Text("shop"))], []) with
    {
        Caller = new(true, [], []),
        GeneratedValues = [Value("later", SemanticValue.Number(10))]
    };

    SemanticExecutionResult Execute(SemanticExecutionRequest request) => new SemanticEvaluator().Execute(_plan, SemanticWorld.Empty, request);

    [Theory]
    [InlineData("-9223372036854775808")]
    [InlineData("9223372036854775807")]
    void should_preserve_exact_typed_numeric_fixtures_at_both_int64_endpoints(string endpoint)
    {
        Compile("concept Ticket : Int\n  generate random -9223372036854775808..9223372036854775807\nmodule M\n  feature F\n    slice StateChange S\n      command Create\n        ticket Ticket generated\n        returns ticket");
        var value = SemanticValue.Number(decimal.Parse(endpoint, System.Globalization.CultureInfo.InvariantCulture));
        var request = SemanticExecutionRequest.Create(Command.Id, [], []) with { GeneratedValues = [Value("ticket", value)] };
        var result = (SemanticAccepted)Execute(request);
        ((SemanticScalarExecutionResponse)result.Response!).Value.ShouldEqual(value);
    }

    [Fact]
    void should_reject_an_invalid_later_fixture_before_reporting_a_missing_earlier_fixture()
    {
        var result = (SemanticRejected)Execute(Request);
        result.Category.ShouldEqual(SemanticRejectionCategory.Contract);
        result.World.Facts.ShouldBeEmpty();
    }

    [Fact]
    void should_report_a_missing_earlier_fixture_after_validating_all_supplied_entries() =>
        ((SemanticUnsupported)Execute(Request with { GeneratedValues = [Value("later", SemanticValue.Number(9))] })).Capability.ShouldEqual(SemanticExecutionCapability.IdentityAllocation);

    [Fact]
    void should_authorize_before_inspecting_phase_three_fixtures() =>
        ((SemanticRejected)Execute(Request with { Caller = null, GeneratedValues = [null!] })).Category.ShouldEqual(SemanticRejectionCategory.Unauthorized);

    [Fact]
    void should_validate_inputs_before_inspecting_phase_three_fixtures() =>
        ((SemanticRejected)Execute(Request with { Values = [Value("name", SemanticValue.Text(""))], GeneratedValues = [null!] })).Category.ShouldEqual(SemanticRejectionCategory.Validation);
}
