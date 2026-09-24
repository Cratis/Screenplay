// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.for_ExecutableSemanticModel;

public class when_a_policy_contains_a_nested_opaque_condition : Specification
{
    [Fact]
    void should_reject_the_nested_opaque_condition()
    {
        var model = canonical_serialization_golden_vectors.CreateSemanticModelV3();
        var policy = model.Application.Policies.Single(value => value.Name == "RequiresTargetPolicy");
        var requirementId = ((SemanticOpaquePolicyCondition)policy.Condition).RequirementId;
        var nested = policy with
        {
            Condition = new SemanticLogicalPolicyCondition(
                new SemanticOpaquePolicyCondition(requirementId),
                SemanticLogicalOperator.And,
                new SemanticAuthenticatedCondition())
        };
        var application = model.Application with { Policies = model.Application.Policies.Replace(policy, nested) };

        Catch.Exception(() => ExecutableSemanticModel.Create(LanguageVersion.V3, SemanticVersion.V3, application))
            .ShouldBeOfExactType<InvalidSemanticContract>();
    }
}
#endif
