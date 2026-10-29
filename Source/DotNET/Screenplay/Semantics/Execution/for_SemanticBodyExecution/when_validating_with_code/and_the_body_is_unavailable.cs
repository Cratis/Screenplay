// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution.when_validating_with_code;

public class and_the_body_is_unavailable : given.a_code_validation
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    void should_refuse_missing_ambiguous_mismatched_or_wrong_role_bodies(int shape)
    {
        var original = Bodies.Single();
        Bodies = shape switch
        {
            0 => [],
            1 => [original, original],
            2 => [original with { Content = "changed" }],
            3 => [original with { Requirement = original.Requirement with { Role = SemanticImplementationRole.RulePredicate } }],
            _ => [original with { Requirement = original.Requirement with { Role = SemanticImplementationRole.ConceptValidation } }]
        };
        var result = (SemanticUnsupported)Execute(TestContext.Current.CancellationToken);
        result.Details.ShouldContain(original.Requirement.RequirementId);
        result.Details.ShouldContain(shape == 2 ? "content does not match" : "No resolved body");
        Contexts.Count.ShouldEqual(0);
    }
}
