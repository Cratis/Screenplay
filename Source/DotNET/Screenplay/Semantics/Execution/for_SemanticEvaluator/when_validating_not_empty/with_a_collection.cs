// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_validating_not_empty;

public class with_a_collection : given.a_not_empty_plan
{
    [Fact]
    void should_reject_an_empty_collection_in_every_version()
    {
        foreach (var result in ExecuteEveryVersion("RegisterCollection", SemanticValue.Array([])))
        {
            result.ShouldBeOfExactType<SemanticRejected>();
            ((SemanticRejected)result).Category.ShouldEqual(SemanticRejectionCategory.Validation);
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\u0085")]
    void should_accept_a_collection_with_an_empty_text_element_in_every_version(string text)
    {
        foreach (var result in ExecuteEveryVersion("RegisterCollection", SemanticValue.Array([SemanticValue.Text(text)])))
        {
            result.ShouldBeOfExactType<SemanticAccepted>();
        }
    }
}
