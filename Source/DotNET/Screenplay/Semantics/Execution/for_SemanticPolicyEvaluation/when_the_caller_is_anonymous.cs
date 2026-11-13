// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticPolicyEvaluation;

public class when_the_caller_is_anonymous : given.a_plan_gated_command
{
    void Because() => Result = Execute(WorldWithPlan("vessel-1", true), authenticated: false);

    [Fact] void should_refuse_even_though_the_plan_allows_it() => ((SemanticRejected)Result).Category.ShouldEqual(SemanticRejectionCategory.Unauthorized);
}
