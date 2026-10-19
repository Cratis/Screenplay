// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner;

// Decision 0098 (#859): one command appends to the order's source and to the source of each product it names.
public class when_a_command_appends_to_several_event_sources : Specification
{
    const string Source =
        """
        module Sales
          feature Orders
            slice StateChange PlaceOrder
              command PlaceOrder
                orderId String identifier
                productId String
                quantity Int
                produces OrderPlaced
                  for orderId
                  orderId = orderId
                  productId = productId
                produces StockReserved
                  for productId
                  productId = productId
                  quantity = quantity
              event OrderPlaced
                orderId String
                productId String
              event StockReserved
                productId String
                quantity Int
              specification PlacingAnOrderReservesTheProduct
                when PlaceOrder
                  orderId = "o1"
                  productId = "mug"
                  quantity = 3
                then OrderPlaced
                  for "o1"
                  orderId = "o1"
                  productId = "mug"
                then StockReserved
                  for "mug"
                  productId = "mug"
                  quantity = 3
              specification TheReservationNamesItsOwnSource
                when PlaceOrder
                  orderId = "o1"
                  productId = "mug"
                  quantity = 3
                then OrderPlaced
                  for "o1"
                  orderId = "o1"
                  productId = "mug"
                then StockReserved
                  for "o1"
                  productId = "mug"
                  quantity = 3
        """;

    CompilationResult<SemanticCompilation> _compilation;
    SemanticSpecificationRun _reserved;
    SemanticSpecificationRun _wrongSource;

    void Establish()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Sales"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("sales"), "sales", "Sales.play", Source);
        _compilation = new SemanticModelCompiler().Compile("Sales", SemanticDocumentSet.Create([document], catalog));
    }

    void Because()
    {
        var plan = SemanticExecutionPlan.Compile(_compilation.Value!.Model).Plan!;
        var runner = new SemanticSpecificationRunner();
        _reserved = runner.Run(plan, plan.Specifications.Values.Single(_ => _.Name == "PlacingAnOrderReservesTheProduct").Id);
        _wrongSource = runner.Run(plan, plan.Specifications.Values.Single(_ => _.Name == "TheReservationNamesItsOwnSource").Id);
    }

    [Fact] void should_compile() => Assert.True(_compilation.Success, string.Join('\n', _compilation.Diagnostics.Select(_ => _.Message)));
    [Fact] void should_select_esm_v30() => _compilation.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V30);
    [Fact] void should_append_each_event_to_its_own_source() => Assert.True(_reserved.Passed, string.Join('\n', _reserved.Failures));
    [Fact] void should_fail_when_the_stated_source_is_the_wrong_one() => _wrongSource.Passed.ShouldBeFalse();
}
