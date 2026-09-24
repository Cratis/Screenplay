// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner;

public class when_rejecting_a_localized_requirement : Specification
{
    const string Source =
        """
        module Orders
          feature Ordering
            slice StateChange PlaceOrder
              command PlaceOrder
                quantity Int
                validate
                  require quantity > 0
                    message $strings.orders.quantityRequired
              specification NonPositiveQuantity
                when PlaceOrder
                  quantity = 0
                then error "$strings.orders.quantityRequired"
        """;

    SemanticSpecificationRun _run;

    void Because()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Orders"));
        const string key = "localized-requirement";
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument(key), key, "Orders.play", Source);
        var compilation = new SemanticModelCompiler().Compile("Orders", SemanticDocumentSet.Create([document], catalog));
        var plan = SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
        _run = new SemanticSpecificationRunner().Run(plan, plan.Specifications.Values.Single().Id);
    }

    [Fact] void should_pass_the_key_assertion() => _run.Passed.ShouldBeTrue();
    [Fact] void should_mark_the_rejection_as_a_string_key() => ((SemanticRejected)_run.Execution).MessageIsStringKey.ShouldBeTrue();
    [Fact] void should_keep_the_key_verbatim() => ((SemanticRejected)_run.Execution).Details.ShouldEqual("$strings.orders.quantityRequired");
}
