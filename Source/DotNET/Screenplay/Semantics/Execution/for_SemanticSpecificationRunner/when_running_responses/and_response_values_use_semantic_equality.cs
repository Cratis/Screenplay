// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.when_running_responses;

public class and_response_values_use_semantic_equality : given.a_v6_scenario
{
    [Theory]
    [InlineData("none", false)]
    [InlineData("scalar", true)]
    [InlineData("record", false)]
    void should_distinguish_no_response_from_scalar_null_and_record_null(string kind, bool passes)
    {
        Compile("module Billing\n  feature F\n    slice StateChange S\n      command C\n        note String?\n        returns note\n      specification Accepted\n        when C\n          note = \"present\"\n        then returns null");
        var command = _plan.Commands.Values.Single();
        var real = (SemanticAccepted)new SemanticEvaluator().Execute(_plan, SemanticWorld.Empty, SemanticExecutionRequest.Create(command.Id, [new(command.Properties.Single().Id, SemanticValue.Null)], []));
        ((SemanticScalarExecutionResponse)real.Response!).Value.ShouldEqual(SemanticValue.Null);
        SemanticExecutionResponse? response = kind switch
        {
            "scalar" => new SemanticScalarExecutionResponse(SemanticValue.Null),
            "record" => new SemanticRecordExecutionResponse([new("note", SemanticValue.Null)]),
            _ => null
        };
        new SemanticSpecificationRunner(new supplied_response(response)).Run(_plan, _plan.Specifications.Values.Single().Id).Passed.ShouldEqual(passes);
        ((SemanticAccepted)new SemanticEvaluator().EstablishWorld(_plan, [])).Response.ShouldBeNull();
        ((SemanticAccepted)new SemanticEvaluator().Execute(_plan, SemanticWorld.Empty, SemanticExecutionRequest.ForQueries([]))).Response.ShouldBeNull();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_compare_composite_members_by_identity_and_array_elements_in_order(bool reversedArray)
    {
        Compile("type Detail\n  label String\n  tags String[]\nmodule Billing\n  feature F\n    slice StateChange S\n      command C\n        detail Detail\n        returns detail\n      specification Accepted\n        when C\n          detail = {\"label\":\"hello\",\"tags\":[\"a\",\"b\"]}\n        then returns {\"label\":\"hello\",\"tags\":[\"a\",\"b\"]}");
        Run("Accepted").Passed.ShouldBeTrue();
        var scalar = (SemanticScalarSpecificationResponse)_plan.Specifications.Values.Single().ThenReturns!;
        var composite = (SemanticCompositeValue)scalar.Value;
        var value = SemanticValue.Composite([.. composite.Properties.Reverse().Select(property => reversedArray && property.Value is SemanticArrayValue array
            ? property with { Value = SemanticValue.Array([.. array.Values.Reverse()]) }
            : property)]);
        var run = new SemanticSpecificationRunner(new supplied_response(new SemanticScalarExecutionResponse(value))).Run(_plan, _plan.Specifications.Values.Single().Id);
        run.Passed.ShouldEqual(!reversedArray);
    }

    sealed class supplied_response(SemanticExecutionResponse? response) : ISemanticEvaluator
    {
        public SemanticExecutionResult Execute(SemanticExecutionPlan plan, SemanticWorld world, SemanticExecutionRequest request) =>
            new SemanticAccepted(world, [], []) { Response = response };
    }
}
