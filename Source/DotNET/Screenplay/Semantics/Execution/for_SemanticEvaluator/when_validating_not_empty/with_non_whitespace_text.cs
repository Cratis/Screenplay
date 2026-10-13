// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_validating_not_empty;

public class with_non_whitespace_text : given.a_not_empty_plan
{
    [Theory]
    [InlineData("a")]
    [InlineData(" a ")]
    [InlineData("\ufeff")]
    void should_accept_the_concept_rule_in_every_version(string text) => AssertAccepted("RegisterConcept", text);

    [Theory]
    [InlineData("a")]
    [InlineData(" a ")]
    [InlineData("\ufeff")]
    void should_accept_the_property_rule_in_every_version(string text) => AssertAccepted("RegisterProperty", text);

    void AssertAccepted(string command, string text)
    {
        foreach (var result in ExecuteEveryVersion(command, SemanticValue.Text(text)))
        {
            result.ShouldBeOfExactType<SemanticAccepted>();
        }
    }
}
