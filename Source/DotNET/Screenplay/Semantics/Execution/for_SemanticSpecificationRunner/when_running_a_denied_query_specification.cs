// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner;

public class when_running_a_denied_query_specification : Specification
{
    const string Source =
        """
        policy NeedsReader
          require role "Reader"
        module Orders
          feature Ordering
            slice StateView OrderSummary
              readmodel OrderSummary
                orderId Uuid
              query OrderById => OrderSummary?
                by orderId Uuid
                authorize NeedsReader
              specification GuestCannotRead
                given caller
                  authenticated
                  role "Guest"
                then query OrderById
                  arguments
                    orderId = "00000000-0000-0000-0000-000000000001"
                then denied
        """;

    SemanticSpecificationRun _run;

    void Because()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Orders"));
        const string key = "denied-query";
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument(key), key, "Orders.play", Source);
        var compilation = new SemanticModelCompiler().Compile("Orders", SemanticDocumentSet.Create([document], catalog));
        var plan = SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
        _run = new SemanticSpecificationRunner().Run(plan, plan.Specifications.Values.Single().Id);
    }

    [Fact] void should_pass_the_denial_assertion() => _run.Passed.ShouldBeTrue();
    [Fact] void should_return_a_typed_unauthorized_rejection() => ((SemanticRejected)_run.Execution).Category.ShouldEqual(SemanticRejectionCategory.Unauthorized);
}
