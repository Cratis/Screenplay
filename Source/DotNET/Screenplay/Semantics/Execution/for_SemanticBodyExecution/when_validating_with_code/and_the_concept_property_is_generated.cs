// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution.when_validating_with_code;

public class and_the_concept_property_is_generated : given.a_code_validation
{
    void Establish()
    {
        Configure("""
            concept Receipt : Uuid
              validate
                ```csharp
                yield break;
                ```
            module Orders
              feature Ordering
                slice StateChange Place
                  command Place
                    receipt Receipt generated
                    returns receipt
            """,
            "PLAY0689");
    }

    void Because() => Result = Execute();

    [Fact] void should_preserve_the_refusal() => ((SemanticUnsupported)Result).Details.ShouldEqual("Concept 'Receipt' code validation 0 has an opaque validation predicate and requires a target provider.");
    [Fact] void should_not_invoke_the_provider() => Contexts.Count.ShouldEqual(0);
}
