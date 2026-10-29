// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution.when_validating_with_code;

public class and_no_provider_is_supplied : given.a_code_validation
{
    void Because() => Result = new SemanticEvaluator().Execute(Plan, SemanticWorld.Empty, Request);

    [Fact] void should_preserve_the_refusal() => ((SemanticUnsupported)Result).Details.ShouldEqual("Command 'Place' code validation 0 has an opaque validation predicate and requires a target provider.");
}
