// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_authorizing_a_composite_query_with_negated_subject : given.a_semantic_binder
{
    CompilationResult<SemanticCompilation> _bound;
    SemanticExecutionResult _result;

    void Because()
    {
        _bound = Bind("""
            policy NotOwner
              require not claim "owner" matches subject
            module Portal
              feature Reports
                slice StateView Report
                  readmodel ReportView
                    owner String key
                    period String key
                  query GetReport => ReportView optional
                    by
                      owner String
                      period String
                    authorize NotOwner
            """);
        Assert.True(_bound.Success, string.Join('\n', _bound.Diagnostics.Select(diagnostic => diagnostic.Message)));
        var model = _bound.Value!.Model;
        var query = model.Application.Modules.Single().Features.Single().Slices.Single().Queries.Single();
        var key = SemanticValue.Composite([.. query.KeyParts.Select(part => new SemanticPropertyValue(part.KeyProperty, SemanticValue.Text(part.Name)))]);
        var request = SemanticExecutionRequest.ForQueries([new(query.Id, key)]) with
        {
            Caller = new(true, [], [new("owner", "someoneElse")])
        };
        _result = new SemanticEvaluator().Execute(SemanticExecutionPlan.Compile(model).Plan!, SemanticWorld.Empty, request);
    }

    [Fact] void should_bind_without_picking_a_subject_part() => _bound.Success.ShouldBeTrue();
    [Fact] void should_warn_that_the_subject_is_not_scalar() => _bound.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.IndeterminateNegatedClaimTarget).Message.ShouldContain("target 'subject' is not a scalar text type");
    [Fact] void should_deny_the_unknown_subject_under_negation() => ((SemanticRejected)_result).Category.ShouldEqual(SemanticRejectionCategory.Unauthorized);
}
