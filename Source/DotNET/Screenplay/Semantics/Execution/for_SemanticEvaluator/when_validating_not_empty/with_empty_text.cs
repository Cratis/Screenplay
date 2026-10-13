// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_validating_not_empty;

public class with_empty_text : given.a_not_empty_plan
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n\r")]
    [InlineData("\u0085")]
    [InlineData("\u00a0")]
    [InlineData("\u3000")]
    void should_reject_the_concept_rule_in_every_version(string? text) => AssertRejected("RegisterConcept", text);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n\r")]
    [InlineData("\u0085")]
    [InlineData("\u00a0")]
    [InlineData("\u3000")]
    void should_reject_the_property_rule_in_every_version(string? text) => AssertRejected("RegisterProperty", text);

    void AssertRejected(string command, string? text)
    {
        foreach (var result in ExecuteEveryVersion(command, text is null ? SemanticValue.Null : SemanticValue.Text(text)))
        {
            result.ShouldBeOfExactType<SemanticRejected>();
            ((SemanticRejected)result).Category.ShouldEqual(SemanticRejectionCategory.Validation);
            ((SemanticRejected)result).Details.ShouldEqual("Name is required");
            ReferenceEquals(result.World, SemanticWorld.Empty).ShouldBeTrue();
        }
    }
}
