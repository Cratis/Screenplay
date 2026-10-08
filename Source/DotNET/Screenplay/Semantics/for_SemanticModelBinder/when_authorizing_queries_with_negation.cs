// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_authorizing_queries_with_negation : given.a_semantic_binder
{
    SemanticExecutionResult _allowed;
    SemanticExecutionResult _denied;

    void Because()
    {
        var result = Bind("""
            policy NotOwner
              require not claim "owner" matches subject
            module Portal
              feature Reports
                slice StateView Report
                  readmodel ReportView
                    owner String
                  query GetReport => ReportView?
                    by owner String
                    authorize NotOwner
            """);
        Assert.True(result.Success, string.Join('\n', result.Diagnostics.Select(diagnostic => diagnostic.Message)));
        var model = result.Value!.Model;
        var query = model.Application.Modules.Single().Features.Single().Slices.Single().Queries.Single();
        var plan = SemanticExecutionPlan.Compile(model).Plan!;
        var request = SemanticExecutionRequest.ForQueries([new(query.Id, SemanticValue.Text("person"))]);
        var evaluator = new SemanticEvaluator();
        _allowed = evaluator.Execute(plan, SemanticWorld.Empty, request with { Caller = new(true, [], [new("owner", "someoneElse")]) });
        _denied = evaluator.Execute(plan, SemanticWorld.Empty, request with { Caller = new(true, [], [new("OWNER", "person")]) });
    }

    [Fact] void should_allow_a_different_subject() => _allowed.ShouldBeOfExactType<SemanticAccepted>();
    [Fact] void should_deny_a_matching_subject() => ((SemanticRejected)_denied).Category.ShouldEqual(SemanticRejectionCategory.Unauthorized);
}
