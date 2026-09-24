// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.given;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator;

public class when_evaluating_portable_authorization : a_valid_semantic_model
{
    SemanticExecutionResult _withoutCaller;
    SemanticExecutionResult _missingClaim;
    SemanticExecutionResult _matchingClaim;
    SemanticExecutionResult _wrongCaseValue;
    SemanticExecutionResult _roleAlternative;
    SemanticExecutionResult _wrongCaseRole;
    SemanticExecutionResult _invalidInput;
    SemanticExecutionResult _queryDenied;
    SemanticExecutionResult _queryAccepted;

    void Because()
    {
        var feature = _application.Modules.Single().Features.Single();
        var commandSlice = feature.Slices.Single(slice => slice.Commands.Length > 0);
        var querySlice = feature.Slices.Single(slice => slice.Queries.Length > 0);
        var securedCommand = commandSlice.Commands.Single() with
        {
            Authorization = new SemanticPolicyReference("CanRegister")
        };
        var securedQuery = querySlice.Queries.Single() with
        {
            Authorization = new SemanticPolicyReference("OwnsProject")
        };
        var application = ReplaceSlices(
            commandSlice with { Commands = [securedCommand], Specifications = [] },
            querySlice with { Queries = [securedQuery] }) with
        {
            Policies =
            [
                new("CanRegister", new SemanticLogicalPolicyCondition(
                    new SemanticRoleCondition("Admin"), SemanticLogicalOperator.Or,
                    new SemanticLogicalPolicyCondition(new SemanticAuthenticatedCondition(), SemanticLogicalOperator.And,
                        new SemanticClaimCondition("department", SemanticClaimTargetKind.Literal, "Finance")))),
                new("OwnsProject", new SemanticClaimCondition("project", SemanticClaimTargetKind.Subject, null))
            ]
        };
        var plan = SemanticExecutionPlan.Compile(ExecutableSemanticModel.Create(LanguageVersion.V1, SemanticVersion.V1, application)).Plan!;
        var evaluator = new SemanticEvaluator();
        var valid = SemanticExecutionRequest.Create(_commandId,
            [new(_commandProjectIdPropertyId, SemanticValue.Text("00000000-0000-0000-0000-000000000001")),
             new(_commandNamePropertyId, SemanticValue.Text("Screenplay"))], []);
        var invalid = valid with { Values = [new(_commandProjectIdPropertyId, SemanticValue.Text("00000000-0000-0000-0000-000000000001")), new(_commandNamePropertyId, SemanticValue.Text(string.Empty))] };
        var matched = new SemanticCaller(true, [], [new("DEPARTMENT", "Engineering"), new("department", "Finance")]);
        var mismatch = new SemanticCaller(true, [], [new("department", "finance")]);
        _withoutCaller = evaluator.Execute(plan, SemanticWorld.Empty, valid);
        _missingClaim = evaluator.Execute(plan, SemanticWorld.Empty, valid with { Caller = new(true, [], []) });
        _matchingClaim = evaluator.Execute(plan, SemanticWorld.Empty, valid with { Caller = matched });
        _wrongCaseValue = evaluator.Execute(plan, SemanticWorld.Empty, valid with { Caller = mismatch });
        _roleAlternative = evaluator.Execute(plan, SemanticWorld.Empty, valid with { Caller = new(false, ["Admin"], []) });
        _wrongCaseRole = evaluator.Execute(plan, SemanticWorld.Empty, valid with { Caller = new(false, ["admin"], []) });
        _invalidInput = evaluator.Execute(plan, SemanticWorld.Empty, invalid with { Caller = mismatch });
        var key = SemanticValue.Text("00000000-0000-0000-0000-000000000001");
        _queryDenied = evaluator.Execute(plan, SemanticWorld.Empty, SemanticExecutionRequest.ForQueries([new(_queryId, key)]) with { Caller = new(true, [], [new("project", "wrong")]) });
        _queryAccepted = evaluator.Execute(plan, SemanticWorld.Empty, SemanticExecutionRequest.ForQueries([new(_queryId, key)]) with { Caller = new(true, [], [new("PROJECT", "00000000-0000-0000-0000-000000000001")]) });
    }

    [Fact] void should_deny_a_missing_caller() => ((SemanticRejected)_withoutCaller).Category.ShouldEqual(SemanticRejectionCategory.Unauthorized);
    [Fact] void should_deny_a_missing_claim() => ((SemanticRejected)_missingClaim).Category.ShouldEqual(SemanticRejectionCategory.Unauthorized);
    [Fact] void should_accept_any_matching_value_for_case_insensitive_claim_types() => _matchingClaim.ShouldBeOfExactType<SemanticAccepted>();
    [Fact] void should_compare_claim_values_ordinally() => ((SemanticRejected)_wrongCaseValue).Category.ShouldEqual(SemanticRejectionCategory.Unauthorized);
    [Fact] void should_preserve_or_grouping() => _roleAlternative.ShouldBeOfExactType<SemanticAccepted>();
    [Fact] void should_compare_role_names_ordinally() => ((SemanticRejected)_wrongCaseRole).Category.ShouldEqual(SemanticRejectionCategory.Unauthorized);
    [Fact] void should_reject_authorization_before_validation() => ((SemanticRejected)_invalidInput).Category.ShouldEqual(SemanticRejectionCategory.Unauthorized);
    [Fact] void should_deny_an_unmatched_query_subject() => ((SemanticRejected)_queryDenied).Category.ShouldEqual(SemanticRejectionCategory.Unauthorized);
    [Fact] void should_accept_a_matching_query_subject() => _queryAccepted.ShouldBeOfExactType<SemanticAccepted>();
}
