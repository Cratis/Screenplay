// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_policy_with_claim_targets : given.a_compiler
{
    const string Source =
        """
        policy IsInFinance
          require claim "department" matches "Finance"

        policy IsSameDepartment
          require claim "department" matches invoice.department

        policy IsOwnTenant
          require claim "tenantId" matches $context.tenant

        policy IsCustomerSelf
          require claim "customerId" matches subject
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_read_a_quoted_target_as_a_literal() => Claim("IsInFinance").Matches.ShouldBeOfExactType<LiteralExpressionSyntax>();
    [Fact] void should_keep_the_literal_value() => ((LiteralExpressionSyntax)Claim("IsInFinance").Matches!).Value.ShouldEqual("Finance");
    [Fact] void should_read_a_bare_target_as_a_path() => Claim("IsSameDepartment").Matches.ShouldBeOfExactType<PathExpressionSyntax>();
    [Fact] void should_keep_the_path() => ((PathExpressionSyntax)Claim("IsSameDepartment").Matches!).Path.ShouldEqual("invoice.department");
    [Fact] void should_read_a_context_target_as_a_context_expression() => Claim("IsOwnTenant").Matches.ShouldBeOfExactType<ContextExpressionSyntax>();
    [Fact] void should_keep_the_context_path() => ((ContextExpressionSyntax)Claim("IsOwnTenant").Matches!).Path.ShouldEqual("tenant");
    [Fact] void should_read_subject_as_the_subject() => Claim("IsCustomerSelf").MatchesSubject.ShouldBeTrue();
    [Fact] void should_have_no_target_for_the_subject() => Claim("IsCustomerSelf").Matches.ShouldBeNull();

    ClaimConditionSyntax Claim(string policy) =>
        (ClaimConditionSyntax)_result.Value!.Policies.Single(_ => _.Name == policy).Condition!;
}
