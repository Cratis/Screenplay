// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_projecting_scoped_projections;

public class with_deferral_across_given_and_when : for_SemanticModelBinder.given.a_semantic_binder
{
    const string Source =
        """
        concept Id : Uuid
        type Line
          lineNumber Int
          amount Int
        module Orders
          feature Ordering
            slice StateView Lookup
              event LineAdded
                orderId Id
                lineNumber Int
                amount Int
              event OrderAdded
                orderId Id
              readmodel OrderView
                orderId Id
                lines Line[]
              query OrderById => OrderView?
                by orderId Id
              projection Orders => OrderView
                from OrderAdded key orderId
                children lines identified by lineNumber
                  from LineAdded key lineNumber
                    parent orderId
                    amount = amount
              specification ChildBeforeParent
                given LineAdded
                  orderId = "00000000-0000-0000-0000-000000000001"
                  lineNumber = 1
                  amount = 10
                when append OrderAdded
                  orderId = "00000000-0000-0000-0000-000000000001"
                then readmodel OrderView
                  orderId = "00000000-0000-0000-0000-000000000001"
                  lines = [{"lineNumber":1,"amount":10}]
        """;

    SemanticExecutionPlan _plan;
    SemanticSpecificationRun _run;

    void Establish()
    {
        var compilation = Bind(Source);
        compilation.Diagnostics.Where(diagnostic => diagnostic.Severity == Diagnostics.DiagnosticSeverity.Error).Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}").ShouldBeEmpty();
        var plan = SemanticExecutionPlan.Compile(compilation.Value!.Model);
        plan.Issues.Select(issue => issue.Details).ShouldBeEmpty();
        _plan = plan.Plan!;
    }
    void Because() => _run = new SemanticSpecificationRunner().Run(_plan, _plan.Specifications.Values.Single().Id);

    [Fact] void should_run() => _run.Failures.ShouldBeEmpty();
    [Fact] void should_apply_the_given_child_after_the_when_parent_arrives() => _run.Passed.ShouldBeTrue();
}
