// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution;

public class when_no_provider_is_supplied : given.a_rule
{
    void Because() => Result = new SemanticEvaluator().Execute(Plan, SemanticWorld.Empty, Request);

    [Fact] void should_keep_the_existing_refusal() => ((SemanticUnsupported)Result).Details.ShouldEqual("Rule 'valid' on command 'Place' property 'name' has an opaque validation predicate and requires a target provider.");
    [Fact] void should_not_invoke_a_provider() => Context.ShouldBeNull();
}
