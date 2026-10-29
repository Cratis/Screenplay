// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution.when_validating_with_code;

public class and_it_guards_a_query_argument : given.a_code_validation
{
    void Establish()
    {
        Configure("""
            concept Label : String
              validate
                ```csharp
                yield break;
                ```
            module Orders
              feature Ordering
                slice StateChange Place
                  command Place
                slice StateView Labels
                  readmodel LabelView
                    label Label
                  query ByLabel => LabelView optional
                    by label Label
            """);
        Request = SemanticExecutionRequest.ForQueries([new(Plan.Queries.Values.Single().Id, SemanticValue.Text("First"))]);
    }

    void Because() => Result = Execute();

    [Fact] void should_preserve_the_refusal_even_with_a_provider() =>
        ((SemanticUnsupported)Result).Details.ShouldEqual("Concept 'Label' code validation 0 has an opaque validation predicate and requires a target provider.");

    [Fact] void should_not_invoke_the_provider() => ReceivedBodies.ShouldBeEmpty();
}
