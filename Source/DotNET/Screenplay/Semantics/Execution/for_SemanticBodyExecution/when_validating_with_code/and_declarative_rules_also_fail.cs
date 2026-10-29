// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution.when_validating_with_code;

public class and_declarative_rules_also_fail : given.a_code_validation
{
    void Establish()
    {
        Configure("""
            concept Label : String
              validate
                rule first message "Concept predicate first"
                  ```csharp
                  yield break;
                  ```
              validate
                ```csharp
                yield break;
                ```
              validate
                rule last message "Concept predicate last"
                  ```csharp
                  yield break;
                  ```
            module Orders
              feature Ordering
                slice StateChange Place
                  command Place
                    name Label
                    validate
                      name not empty message "Declarative"
                      name rule named message "Named rule"
                        ```csharp
                        yield break;
                        ```
                    validate
                      ```csharp
                      yield break;
                      ```
                    validate
                      ```csharp
                      yield break;
                      ```
                    produces Placed
                  event Placed
            """);
        Request = Request with { Values = [new(Request.Values.Single().TargetProperty, SemanticValue.Text(string.Empty))] };
        var call = 0;
        Provider = new((_, context) =>
        {
            Contexts.Add(context);
            return SemanticValidationBodyResult.Invalid($"Code {++call}");
        }) { PredicateAnswer = new(false) };
    }

    void Because() => Result = Execute();

    [Fact] void should_preserve_the_declared_failure_order() => ((SemanticRejected)Result).ValidationFailures.Select(failure => failure.Message).ShouldEqual(["Declarative", "Named rule", "Code 1", "Code 2", "Concept predicate first", "Code 3", "Concept predicate last"]);
    [Fact] void should_invoke_each_command_block_and_concept_block_once() => Contexts.Count.ShouldEqual(3);
}
