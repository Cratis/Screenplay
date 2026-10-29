// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution.when_validating_with_code;

public class and_the_result_is_malformed : given.a_code_validation
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    void should_refuse_missing_default_or_null_messages(int shape)
    {
        Answer = shape switch
        {
            0 => new(null),
            1 => new(default(ImmutableArray<string>)),
            2 => new([null!]),
            _ => null!
        };
        var result = (SemanticUnsupported)Execute(TestContext.Current.CancellationToken);
        result.Details.ShouldContain(Bodies.Single().Requirement.RequirementId);
        result.Details.ShouldContain("The provider supplied no validation result.");
    }
}
