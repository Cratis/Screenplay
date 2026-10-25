// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.when_a_slice_publishes_what_another_slice_appends.given;

// A specification is about its own slice only: the outbound Translate slice owns the public event, so only its
// specification states it, and the command's specification states only what the command produces.
public class a_publishing_model : Specification
{
    protected const string Model =
        """
        module Catalog
          feature Pricing
            slice StateChange SetPrice
              command SetPrice
                productId String identifier
                price Decimal
                produces PriceSet
                  for productId
                  price = price
              event PriceSet
                price Decimal
              specification SettingAPrice
                when SetPrice
                  productId = "p1"
                  price = 59
                then PriceSet
                  price = 59
            slice Translate PublishPrice
              direction outbound
              public event PriceChanged
                productId String
                newPrice Decimal
              projection PricePublisher => PriceChanged
                from PriceSet
                  productId = $eventSourceId
                  newPrice = price
              specification APriceIsPublished
                when append PriceSet
                  for "p1"
                  price = 59
                then PriceChanged
                  productId = "p1"
                  newPrice = 59
              specification ThePublicationMustMatch
                when append PriceSet
                  for "p1"
                  price = 59
                then PriceChanged
                  productId = "p1"
                  newPrice = 60
        """;

    protected static CompilationResult<SemanticCompilation> Compile(string source)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Catalog"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("catalog"), "catalog", "Catalog.play", source);
        return new SemanticModelCompiler().Compile("Catalog", SemanticDocumentSet.Create([document], catalog));
    }

    protected static SemanticSpecificationRun Run(CompilationResult<SemanticCompilation> compilation, string specification)
    {
        Assert.True(compilation.Success, string.Join('\n', compilation.Diagnostics.Select(_ => _.Message)));
        var plan = SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
        return new SemanticSpecificationRunner().Run(plan, plan.Specifications.Values.Single(_ => _.Name == specification).Id);
    }
}
