// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_repeated_construct_authorization : given.a_semantic_binder
{
    const string Source =
        """
        policy Staff
          require role "Staff"
        policy Finance
          require role "Finance"
        module Portal
          feature Reports
            slice StateChange FileReport
              command FileReport
                authorize Staff
                authorize Finance
              readmodel Report
                id Uuid
              query ReportById => Report?
                by id Uuid
                authorize Staff
                authorize Finance
        """;

    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(Source);

    [Fact] void should_bind_without_diagnostics() => _result.Diagnostics.ShouldBeEmpty();

    [Fact] void should_deny_a_caller_satisfying_only_one_command_gate() => AssertDenied(false);
    [Fact] void should_deny_a_caller_satisfying_only_one_query_gate() => AssertDenied(true);

    void AssertDenied(bool query)
    {
        var plan = SemanticExecutionPlan.Compile(_result.Value!.Model).Plan!;
        var authorization = query ? plan.Queries.Values.Single().Authorization : plan.Commands.Values.Single().Authorization;
        var argument = plan.Queries.Values.Single().Argument;
        IEnumerable<SemanticProperty> properties = query ? [new(argument.Id, argument.Name, argument.Type, false)] : plan.Commands.Values.Single().Properties;
        var decision = SemanticPolicyEvaluation.Evaluate(
            authorization,
            plan,
            new(true, ["Staff"], []),
            new Dictionary<string, SemanticValue>(),
            null,
            properties);
        decision.Outcome.ShouldEqual(SemanticPolicyOutcome.Deny);
    }
}
