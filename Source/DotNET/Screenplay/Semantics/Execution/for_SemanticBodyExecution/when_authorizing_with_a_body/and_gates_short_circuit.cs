// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution.when_authorizing_with_a_body;

public class and_gates_short_circuit : given.a_policy
{
    [Theory]
    [InlineData("Member and Permitted", false, "allow", "deny", 0)]
    [InlineData("Member or Permitted", true, "deny", "allow", 0)]
    [InlineData("Permitted and Member", false, "unsupported", "unsupported", 1)]
    [InlineData("Permitted or Member", true, "unsupported", "unsupported", 1)]
    [InlineData("Permitted and Member", false, "allow", "deny", 1)]
    [InlineData("Permitted or Member", true, "deny", "allow", 1)]
    void should_preserve_authored_order(string gate, bool authenticated, string answer, string expected, int invocations)
    {
        Contexts.Clear();
        Configure(Source.Replace("authorize Permitted", $"authorize {gate}", StringComparison.Ordinal));
        Request = Request with { Caller = new(authenticated, [], []) };
        Answer = answer switch
        {
            "allow" => SemanticPolicyBodyResult.Allow(),
            "deny" => SemanticPolicyBodyResult.Deny(),
            _ => SemanticPolicyBodyResult.Unsupported("unavailable")
        };
        var result = Execute();
        var outcome = result switch
        {
            SemanticAccepted => "allow",
            SemanticRejected { Category: SemanticRejectionCategory.Unauthorized } => "deny",
            _ => "unsupported"
        };
        outcome.ShouldEqual(expected);
        Contexts.Count.ShouldEqual(invocations);
    }

    [Theory]
    [InlineData(false, 0)]
    [InlineData(true, 3)]
    void should_keep_inherited_and_repeated_gates_in_order(bool authenticated, int invocations)
    {
        Contexts.Clear();
        Answer = SemanticPolicyBodyResult.Allow();
        Configure(Source.Replace("module Orders", "module Orders\n  authorize Member", StringComparison.Ordinal)
            .Replace("feature Ordering", "feature Ordering\n    authorize Permitted", StringComparison.Ordinal)
            .Replace("        authorize Permitted", "        authorize Permitted\n        authorize Permitted", StringComparison.Ordinal));
        Request = Request with { Caller = new(authenticated, [], []) };
        var result = Execute();
        if (authenticated) result.ShouldBeOfExactType<SemanticAccepted>();
        else ((SemanticRejected)result).Category.ShouldEqual(SemanticRejectionCategory.Unauthorized);
        Contexts.Count.ShouldEqual(invocations);
    }

}
