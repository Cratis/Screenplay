// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax.Specifications.for_PersonaCallers;

public class when_synthesizing_policy_witnesses : Specification
{
    [Fact] void should_always_authenticate_a_role_only_persona() => Synthesize("role \"Accountant\"").Caller!.Authenticated.ShouldBeTrue();
    [Fact] void should_choose_only_the_leftmost_buildable_alternative() => Synthesize("role \"A\" or role \"B\"").Caller!.Roles.ShouldContainOnly("A");
    [Fact] void should_skip_a_nonliteral_alternative() => Synthesize("claim \"owner\" matches subject or role \"B\"").Caller!.Roles.ShouldContainOnly("B");
    [Fact] void should_refuse_a_required_nonliteral_claim() => Synthesize("role \"A\" and claim \"owner\" matches subject").Refusal!.Reason.ShouldEqual("nonLiteralClaim");
    [Fact] void should_refuse_negation_even_in_an_unused_alternative() => Synthesize("role \"A\" or not role \"B\"").Refusal!.Reason.ShouldEqual("negation");
    [Fact] void should_refuse_a_required_role_claim_ignoring_case() => Synthesize("claim \"HTTP://SCHEMAS.MICROSOFT.COM/WS/2008/06/IDENTITY/CLAIMS/ROLE\" matches \"A\"").Refusal!.Reason.ShouldEqual("roleClaim");
    [Fact] void should_skip_an_alternative_needing_a_role_claim() => Synthesize("claim \"http://schemas.microsoft.com/ws/2008/06/identity/claims/role\" matches \"A\" or role \"B\"").Caller!.Roles.ShouldContainOnly("B");
    [Fact] void should_report_the_first_unbuildable_alternative() => Synthesize("claim \"http://schemas.microsoft.com/ws/2008/06/identity/claims/role\" matches \"A\" or claim \"owner\" matches subject").Refusal!.Reason.ShouldEqual("roleClaim");
    [Fact] void should_allow_a_bare_role_claim_type() => Synthesize("claim \"role\" matches \"A\"").Caller!.Claims.Single().Type.ShouldEqual("role");
    [Fact] void should_not_count_unbuildable_alternatives_as_ambiguous() => Synthesize("claim \"http://schemas.microsoft.com/ws/2008/06/identity/claims/role\" matches \"A\" or role \"B\"").Ambiguities.ShouldBeEmpty();
    [Fact] void should_not_report_ambiguity_in_an_unbuildable_alternative() => Synthesize("role \"A\" or ((role \"B\" or role \"C\") and claim \"owner\" matches subject)").Ambiguities.ShouldBeEmpty();
    [Fact] void should_handle_long_alternative_chains_without_reexpanding_the_chosen_branch() => Synthesize(string.Join(" or ", Enumerable.Range(0, 40).Select(index => $"role \"R{index}\""))).Caller!.Roles.ShouldContainOnly("R0");
    [Fact] void should_report_buildable_alternatives() => Synthesize("role \"A\" or role \"B\"").Ambiguities.Count.ShouldEqual(1);
    [Fact] void should_collect_required_atoms_before_resolving_any_alternative() => Synthesize("role \"A\" or role \"B\"", "role \"B\"").Caller!.Roles.ShouldContainOnly("B");
    [Fact] void should_not_report_a_pinned_alternative() => Synthesize("role \"A\" or role \"B\"", "role \"B\"").Ambiguities.ShouldBeEmpty();
    [Fact] void should_sort_and_deduplicate_roles() => Synthesize("role \"Z\" and role \"A\" and role \"Z\"").Caller!.Roles.ShouldContainOnly("A", "Z");
    [Fact] void should_deduplicate_claim_types_ignoring_case() => Synthesize("claim \"dept\" matches \"A\" and claim \"DEPT\" matches \"A\"").Caller!.Claims.Count().ShouldEqual(1);
    [Fact] void should_not_classify_a_dotless_i_uri_as_a_role_claim() => Synthesize("claim \"http://schemas.microsoft.com/ws/2008/06/identıty/claims/role\" matches \"A\"").Refusal.ShouldBeNull();
    [Fact] void should_not_classify_a_long_s_uri_as_a_role_claim() => Synthesize("claim \"http://ſchemas.microsoft.com/ws/2008/06/identity/claims/role\" matches \"A\"").Refusal.ShouldBeNull();
    [Fact] void should_not_expand_claim_type_casing() => Synthesize("claim \"ß\" matches \"A\" and claim \"SS\" matches \"A\"").Caller!.Claims.Count().ShouldEqual(2);
    [Fact] void should_refuse_a_persona_without_policies() => Synthesize().Refusal!.Reason.ShouldEqual("noPolicies");

    static PersonaCallerResult Synthesize(params string[] conditions)
    {
        var policies = string.Join('\n', conditions.Select((condition, index) => $"policy P{index}\n  require {condition}"));
        var references = string.Join('\n', conditions.Select((_, index) => $"  policy P{index}"));
        var compilation = new ScreenplayCompiler().Compile($"{policies}\npersona Person\n{references}");
        compilation.Success.ShouldBeTrue();
        return PersonaCallers.Synthesize(compilation.Value!.Personas!.Single(), compilation.Value);
    }
}
