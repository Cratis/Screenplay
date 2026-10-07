// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_negating_undecidable_claim_targets : given.a_semantic_binder
{
    [Fact] void should_deny_an_omitted_optional_value() => Denied("not claim \"owner\" matches owner");
    [Fact] void should_deny_a_null_artifact_value() => Denied("not claim \"owner\" matches owner", SemanticValue.Null);
    [Fact] void should_deny_an_absent_subject() => Denied("not claim \"owner\" matches subject");
    [Fact] void should_deny_a_numeric_target() => Denied("not claim \"owner\" matches owner", SemanticValue.Number(42), "Int");
    [Fact] void should_deny_a_boolean_target() => Denied("not claim \"owner\" matches owner", SemanticValue.Boolean(true), "Bool");
    [Fact] void should_keep_double_negation_of_unknown_denied() => Denied("not not claim \"owner\" matches owner");
    [Fact] void should_keep_a_negated_unknown_group_denied() => Denied("not (role \"A\" or claim \"owner\" matches owner)");
    [Fact] void should_allow_a_role_before_an_unknown_alternative() => Evaluate("role \"A\" or not claim \"owner\" matches owner", SemanticValue.Null, roles: ["A"]).ShouldBeOfExactType<SemanticAccepted>();
    [Fact] void should_allow_a_role_after_an_unknown_alternative() => Evaluate("not claim \"owner\" matches owner or role \"A\"", SemanticValue.Null, roles: ["A"]).ShouldBeOfExactType<SemanticAccepted>();
    [Fact] void should_deny_unknown_or_false() => Denied("not claim \"owner\" matches owner or role \"A\"");
    [Fact] void should_deny_false_or_unknown() => Denied("role \"A\" or not claim \"owner\" matches owner");
    [Fact] void should_keep_true_and_unknown_unknown() => Denied("not (authenticated and claim \"owner\" matches owner)");
    [Fact] void should_keep_unknown_and_true_unknown() => Denied("not (claim \"owner\" matches owner and authenticated)");
    [Fact] void should_make_false_and_unknown_false() => Evaluate("not (role \"A\" and claim \"owner\" matches owner)", SemanticValue.Null).ShouldBeOfExactType<SemanticAccepted>();
    [Fact] void should_make_unknown_and_false_false() => Evaluate("not (claim \"owner\" matches owner and role \"A\")", SemanticValue.Null).ShouldBeOfExactType<SemanticAccepted>();
    [Fact] void should_keep_unknown_and_unknown_unknown() => Denied("not (claim \"owner\" matches owner and claim \"owner\" matches subject)");
    [Fact] void should_keep_unknown_or_unknown_unknown() => Denied("not (claim \"owner\" matches owner or claim \"owner\" matches subject)");

    [Fact]
    void should_preserve_positive_policy_denials_for_undecidable_targets()
    {
        Denied("claim \"owner\" matches owner");
        Denied("claim \"owner\" matches owner", SemanticValue.Null);
        Denied("claim \"owner\" matches subject");
        Denied("claim \"owner\" matches owner", SemanticValue.Number(42), "Int");
        Denied("authenticated and claim \"owner\" matches owner");
        Denied("role \"A\" or claim \"owner\" matches owner");
    }

    [Fact] void should_preserve_positive_policy_allowance_with_a_decisive_role() => Evaluate("claim \"owner\" matches owner or role \"A\"", SemanticValue.Null, roles: ["A"]).ShouldBeOfExactType<SemanticAccepted>();

    [Fact]
    void should_authorize_a_role_alternative_with_an_omitted_target_before_request_validation()
    {
        var model = Model("role \"A\" or not claim \"owner\" matches owner", "String optional");
        var command = model.Application.Modules.Single().Features.Single().Slices.Single().Commands.Single();
        var decision = SemanticPolicyEvaluation.Evaluate(command.Authorization, SemanticExecutionPlan.Compile(model).Plan!, new(true, ["A"], []), new Dictionary<string, SemanticValue>(), null, command.Properties);
        decision.Outcome.ShouldEqual(SemanticPolicyOutcome.Allow);
    }

    ExecutableSemanticModel Model(string condition, string type)
    {
        var result = Bind($"""
            policy Access
              require {condition}
            module Portal
              feature Reports
                slice StateChange FileReport
                  command FileReport
                    owner {type}
                    authorize Access
            """);
        Assert.True(result.Success, string.Join('\n', result.Diagnostics.Select(diagnostic => diagnostic.Message)));

        return result.Value!.Model;
    }

    SemanticExecutionResult Evaluate(string condition, SemanticValue? value = null, string type = "String optional", string[]? roles = null)
    {
        var model = Model(condition, type);
        var command = model.Application.Modules.Single().Features.Single().Slices.Single().Commands.Single();
        var request = SemanticExecutionRequest.Create(command.Id, value is null ? [] : [new(command.Properties.Single().Id, value)], []) with
        {
            Caller = new(true, [.. roles ?? []], [new("owner", "person")])
        };

        return new SemanticEvaluator().Execute(SemanticExecutionPlan.Compile(model).Plan!, SemanticWorld.Empty, request);
    }

    void Denied(string condition, SemanticValue? value = null, string type = "String optional") =>
        ((SemanticRejected)Evaluate(condition, value, type)).Category.ShouldEqual(SemanticRejectionCategory.Unauthorized);
}
