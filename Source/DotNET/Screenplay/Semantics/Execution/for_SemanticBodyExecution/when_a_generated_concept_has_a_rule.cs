// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution;

public class when_a_generated_concept_has_a_rule : given.a_rule
{
    SemanticExecutionResult _withoutProvider = null!;

    void Establish()
    {
        Configure(
            """
            concept Receipt : Uuid
              validate
                rule valid
                  ```csharp
                  return true;
                  ```
            module Orders
              feature Ordering
                slice StateChange Place
                  command Place
                    receipt Receipt generated
                    returns receipt
            """,
            "PLAY0689");
        Request = Request with { Values = [], GeneratedValues = [new(Request.Values.Single().TargetProperty, SemanticValue.Text("3ce360fc-022b-47a7-a5ca-173ce9b8e489"))] };
    }

    void Because()
    {
        Result = new SemanticEvaluator(Provider, Bodies).Execute(Plan, SemanticWorld.Empty, Request);
        _withoutProvider = new SemanticEvaluator().Execute(Plan, SemanticWorld.Empty, Request);
    }

    [Fact] void should_remain_unsupported_with_a_provider() => Result.ShouldBeOfExactType<SemanticUnsupported>();
    [Fact] void should_match_the_no_provider_refusal() => ((SemanticUnsupported)Result).Details.ShouldEqual(((SemanticUnsupported)_withoutProvider).Details);
    [Fact] void should_not_evaluate_generated_values() => Context.ShouldBeNull();
}
