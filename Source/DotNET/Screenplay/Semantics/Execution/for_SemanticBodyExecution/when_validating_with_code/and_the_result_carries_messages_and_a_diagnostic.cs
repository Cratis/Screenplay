// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution.when_validating_with_code;

public class and_the_result_carries_messages_and_a_diagnostic : given.a_code_validation
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_refuse_even_when_messages_are_present(bool empty)
    {
        Answer = new(empty ? [] : ["Rejected"], "Not executable");
        var result = (SemanticUnsupported)Execute(TestContext.Current.CancellationToken);
        result.Details.ShouldContain("Not executable");
        result.Details.ShouldContain(Bodies.Single().Requirement.RequirementId);
    }
}
