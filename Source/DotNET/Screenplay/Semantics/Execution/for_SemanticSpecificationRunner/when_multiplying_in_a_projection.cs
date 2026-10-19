// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner;

// 'add Total by unitPrice * quantity' (decision 0098, ESM v29): the operand is the exact product of two event values.
public class when_multiplying_in_a_projection : Specification
{
    const string Source =
        """
        concept BasketId : Uuid
        concept Money : Decimal
          validate
            decimals <= 2
        module Baskets
          feature Shopping
            slice StateChange PutInBasket
              command PutInBasket
                basketId BasketId identifier
                unitPrice Money
                quantity Int
                produces LineAdded
                  for basketId
                  basketId = basketId
                  unitPrice = unitPrice
                  quantity = quantity
              event LineAdded
                basketId BasketId
                unitPrice Money
                quantity Int
              specification AddingOneLine
                when PutInBasket
                  basketId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  unitPrice = 19.99
                  quantity = 3
                then LineAdded
                  basketId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  unitPrice = 19.99
                  quantity = 3
                then readmodel Basket
                  basketId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  total = 59.97
              specification AddingTwoLines
                given LineAdded
                  basketId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  unitPrice = 5
                  quantity = 2
                when PutInBasket
                  basketId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  unitPrice = 0.5
                  quantity = 4
                then LineAdded
                  basketId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  unitPrice = 0.5
                  quantity = 4
            slice StateView BasketLookup
              readmodel Basket
                basketId BasketId
                total Money?
              query BasketById => Basket?
                by basketId BasketId
              projection BasketProjection => Basket
                from LineAdded
                  add total by unitPrice * quantity
        """;

    CompilationResult<SemanticCompilation> _compilation;
    SemanticSpecificationRun _run;

    void Establish()
    {
        const string StableKey = "basket";
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Baskets"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument(StableKey), StableKey, "Baskets.play", Source);
        _compilation = new SemanticModelCompiler().Compile("Baskets", SemanticDocumentSet.Create([document], catalog));
    }

    void Because()
    {
        var plan = SemanticExecutionPlan.Compile(_compilation.Value!.Model).Plan!;
        _run = new SemanticSpecificationRunner().Run(plan, plan.Specifications.Values.Single(_ => _.Name == "AddingOneLine").Id);
    }

    [Fact] void should_compile() => Assert.True(_compilation.Success, string.Join('\n', _compilation.Diagnostics.Select(_ => _.Message)));
    [Fact] void should_select_esm_v29() => _compilation.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V29);
    [Fact] void should_keep_the_factor_in_the_model() => _compilation.Value!.Model.Application.Modules.SelectMany(_ => _.Features).SelectMany(_ => _.Slices).SelectMany(_ => _.Projections).Single().Scope!.From.Single().Mappings.Single().Factor.ShouldNotBeNull();
    [Fact] void should_project_the_product() => Assert.True(_run.Passed, string.Join('\n', _run.Failures));
    [Fact] void should_round_trip_through_the_canonical_json() => Serialization.SemanticModelSerializer.Deserialize(Serialization.SemanticModelSerializer.Serialize(_compilation.Value!.Model)).Revision.ShouldEqual(_compilation.Value.Model.Revision);
}
