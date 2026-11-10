// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticPolicyEvaluation;

public class when_the_plan_excludes_the_feature : given.a_plan_gated_command
{
    void Because() => Result = Execute(WorldWithPlan("vessel-1", false));

    [Fact] void should_refuse_the_command_as_unauthorized() => ((SemanticRejected)Result).Category.ShouldEqual(SemanticRejectionCategory.Unauthorized);
}
