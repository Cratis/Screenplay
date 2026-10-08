// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_policy_negation : given.a_semantic_binder
{
    [Fact] void should_select_v7_for_negation() => Model("not role \"Service\"").SemanticVersion.ShouldEqual(SemanticVersion.V7);
    [Fact] void should_leave_positive_conditions_at_v1() => Model("role \"Service\"").SemanticVersion.ShouldEqual(SemanticVersion.V1);
    [Fact] void should_allow_a_missing_role() => Evaluate("not role \"Service\"", new(true, [], [])).ShouldBeOfExactType<SemanticAccepted>();
    [Fact] void should_deny_a_present_role() => Denied("not role \"Service\"", new(true, ["Service"], []));
    [Fact] void should_preserve_case_sensitive_roles() => Evaluate("not role \"Service\"", new(true, ["service"], [])).ShouldBeOfExactType<SemanticAccepted>();
    [Fact] void should_allow_a_missing_claim() => Evaluate("not claim \"kind\" matches \"service\"", new(true, [], [])).ShouldBeOfExactType<SemanticAccepted>();
    [Fact] void should_deny_any_matching_claim_value() => Denied("not claim \"kind\" matches \"service\"", new(true, [], [new("KIND", "person"), new("kind", "service")]));
    [Fact] void should_preserve_case_sensitive_claim_values() => Evaluate("not claim \"kind\" matches \"service\"", new(true, [], [new("kind", "Service")])).ShouldBeOfExactType<SemanticAccepted>();
    [Fact] void should_negate_authentication() => Evaluate("not authenticated", new(false, [], [])).ShouldBeOfExactType<SemanticAccepted>();
    [Fact] void should_deny_authentication_under_negation() => Denied("not authenticated", new(true, [], []));
    [Fact] void should_not_imply_authentication() => Evaluate("not role \"Service\"", new(false, [], [])).ShouldBeOfExactType<SemanticAccepted>();
    [Fact] void should_keep_absent_caller_denied() => Denied("not authenticated", null);
    [Fact] void should_preserve_not_and_or_precedence() => Evaluate("not role \"Service\" and authenticated or role \"Controller\"", new(false, ["Service", "Controller"], [])).ShouldBeOfExactType<SemanticAccepted>();
    [Fact] void should_preserve_parentheses() => Denied("not (role \"Service\" or authenticated)", new(true, [], []));
    [Fact] void should_preserve_double_negation() => Evaluate("not not authenticated", new(true, [], [])).ShouldBeOfExactType<SemanticAccepted>();
    [Fact] void should_validate_artifact_paths_under_negation() => Bind(Source("not claim \"owner\" matches missing")).Diagnostics.Any(diagnostic => diagnostic.Code == "PLAY0273").ShouldBeTrue();
    [Fact] void should_deny_an_artifact_match() => Denied("not claim \"owner\" matches owner", new(true, [], [new("owner", "person")]));
    [Fact] void should_allow_an_artifact_mismatch() => Evaluate("not claim \"owner\" matches owner", new(true, [], [new("owner", "someoneElse")])).ShouldBeOfExactType<SemanticAccepted>();

    SemanticExecutionResult Evaluate(string condition, SemanticCaller? caller)
    {
        var model = Model(condition);
        var command = model.Application.Modules.Single().Features.Single().Slices.Single().Commands.Single();
        var request = SemanticExecutionRequest.Create(command.Id, [new(command.Properties.Single().Id, SemanticValue.Text("person"))], []) with { Caller = caller };
        return new SemanticEvaluator().Execute(SemanticExecutionPlan.Compile(model).Plan!, SemanticWorld.Empty, request);
    }

    void Denied(string condition, SemanticCaller? caller) => ((SemanticRejected)Evaluate(condition, caller)).Category.ShouldEqual(SemanticRejectionCategory.Unauthorized);

    ExecutableSemanticModel Model(string condition)
    {
        var result = Bind(Source(condition));
        Assert.True(result.Success, string.Join('\n', result.Diagnostics.Select(diagnostic => diagnostic.Message)));
        return result.Value!.Model;
    }

    static string Source(string condition) => $"""
        policy Access
          require {condition}
        module Portal
          feature Reports
            slice StateChange FileReport
              command FileReport
                owner String
                authorize Access
        """;
}
