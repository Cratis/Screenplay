// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_a_reducer;

// A reducer with a body binds an opaque transition contract, not an executable transition.
public class with_a_body : given.a_semantic_binder
{
    const string Source =
        """
        module Billing
          feature Accounts
            slice StateView Balance
              event AmountDeposited
                amount Decimal

              event AmountWithdrawn
                amount Decimal

              readmodel AccountBalance
                balance Decimal
                id Uuid

              query BalanceById => AccountBalance?
                by id Uuid

              reducer Balance => AccountBalance
                on AmountDeposited
                  csharp
                    ```
                    return new(context.Event.amount);
                    ```
                on AmountWithdrawn
                  file Reducers/Withdrawn.cs
        """;

    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(Source);

    [Fact] void should_bind_the_reducer() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_activate_v3() => _result.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V3);
    [Fact] void should_key_by_event_source() => Reducer.Key.ShouldEqual(SemanticReducerKey.EventSourceId);
    [Fact] void should_start_with_null_state() => Reducer.InitialState.ShouldBeNull();
    [Fact] void should_treat_null_result_as_deletion() => Reducer.Result.ShouldEqual(SemanticReducerResult.StateOrDelete);
    [Fact] void should_reference_each_requirement() => Reducer.Transitions.Select(value => value.RequirementId).ShouldEqual(_result.ImplementationRequirements.Select(value => value.RequirementId));
    [Fact] void should_name_each_consumed_event() => Reducer.Transitions.Select(value => value.EventContract).ShouldEqual(_result.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single().Events.Select(value => value.Id));
    [Fact] void should_require_pure_capability() => _result.ImplementationRequirements.All(value => value.RequiredCapability == "pure").ShouldBeTrue();
    [Fact] void should_block_reference_execution_with_a_typed_target_requirement() => Execution.SemanticExecutionPlan.Compile(_result.Value!.Model).Issues.Single().Kind.ShouldEqual(Execution.SemanticPlanIssueKind.RequiresTargetReducer);
    [Fact] void should_list_each_transition_body() => _result.ImplementationRequirements.Select(value => value.Role).ShouldEqual([SemanticImplementationRole.ReducerTransition, SemanticImplementationRole.ReducerTransition]);
    [Fact] void should_preserve_the_inline_language_and_file_path() => _result.ImplementationRequirements.Select(value => (value.Language, value.File)).ShouldEqual([("csharp", null), (null, "Reducers/Withdrawn.cs")]);

    SemanticReducer Reducer => _result.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single().Reducers.Single();
}
