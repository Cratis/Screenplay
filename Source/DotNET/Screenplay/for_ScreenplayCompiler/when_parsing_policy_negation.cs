// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_parsing_policy_negation : Specification
{
    readonly ScreenplayCompiler _compiler = new();

    [Fact] void should_bind_not_tighter_than_and_and_or() => Describe(Parse("not role \"Service\" and authenticated or role \"Controller\"")).ShouldEqual("((not role:Service And authenticated) Or role:Controller)");
    [Fact] void should_negate_parenthesized_groups() => Describe(Parse("not (role \"Service\" or claim \"actorKind\" matches \"service\") and authenticated")).ShouldEqual("(not (role:Service Or claim:actorKind) And authenticated)");
    [Fact] void should_nest_repeated_negation() => Describe(Parse("not not authenticated")).ShouldEqual("not not authenticated");
    [Fact] void should_negate_claim_paths() => ((NotPolicyConditionSyntax)Parse("not claim \"owner\" matches owner")).Operand.ShouldBeOfExactType<ClaimConditionSyntax>();
    [Fact] void should_refuse_a_missing_operand() => _compiler.Parse("policy Access\n  require not").Diagnostics.Any(diagnostic => diagnostic.Code == "PLAY0116").ShouldBeTrue();
    [Fact] void should_refuse_negated_policy_names() => _compiler.Parse("policy Access\n  require not OpaquePolicy").Diagnostics.Any(diagnostic => diagnostic.Code == "PLAY0115").ShouldBeTrue();
    [Fact] void should_refuse_an_unclosed_negated_group() => _compiler.Parse("policy Access\n  require not (authenticated").Diagnostics.Any(diagnostic => diagnostic.Code == "PLAY0117").ShouldBeTrue();

    [Fact]
    void should_preserve_negation_and_grouping_through_printing()
    {
        foreach (var condition in new[] { "not role \"Service\" and authenticated or role \"Controller\"", "not (role \"Service\" or authenticated)", "not not authenticated", "not claim \"actorKind\" matches \"service\"" })
        {
            var syntax = Parse(condition);
            Describe(Parse(ScreenplaySyntaxText.PolicyCondition(syntax))).ShouldEqual(Describe(syntax));
        }
    }

    PolicyConditionSyntax Parse(string condition)
    {
        var result = _compiler.Parse($"policy Access\n  require {condition}");
        result.Diagnostics.ShouldBeEmpty();
        return result.Value!.Policies.Single().Condition!;
    }

    static string Describe(PolicyConditionSyntax condition) => condition switch
    {
        AuthenticatedConditionSyntax => "authenticated",
        RoleConditionSyntax role => $"role:{role.Role}",
        ClaimConditionSyntax claim => $"claim:{claim.Claim}",
        NotPolicyConditionSyntax not => $"not {Describe(not.Operand)}",
        LogicalPolicyConditionSyntax logical => $"({Describe(logical.Left)} {logical.Operator} {Describe(logical.Right)})",
        _ => "unknown"
    };
}
