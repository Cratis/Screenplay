// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution.when_authorizing_with_a_body;

public class and_authorizing_queries : given.a_policy
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_keep_query_denial_transactional(bool readOnly)
    {
        Configure(Source);
        Request = readOnly ? QueryRequest() : Request with { Queries = QueryRequest().Queries };
        var calls = 0;
        Provider = new((_, _) => ++calls == 1 && !readOnly ? SemanticPolicyBodyResult.Allow() : SemanticPolicyBodyResult.Deny());
        var result = Execute();
        ((SemanticRejected)result).Category.ShouldEqual(SemanticRejectionCategory.Unauthorized);
        ReferenceEquals(result.World, SemanticWorld.Empty).ShouldBeTrue();
        result.World.Facts.ShouldBeEmpty();
    }
}
