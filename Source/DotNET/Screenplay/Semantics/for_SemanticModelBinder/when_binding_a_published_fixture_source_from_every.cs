// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_a_published_fixture_source_from_every : given.a_semantic_binder
{
    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind("""
        module M
          feature F
            slice StateChange Pricing
              command ChangePrice
                product Uuid identifier
                price Decimal
                produces PriceChanged
                  for product
                  price = price
              event PriceChanged
                price Decimal
            slice Translate Publication
              direction outbound
              public event PricePublished
                price Decimal
              projection Publisher => PricePublished
                every
                  price = 10.5
              specification Publishing
                when append PriceChanged
                  for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  price = 10.5
                then PricePublished
                  for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  price = 10.5
        """);

    [Fact] void should_bind_the_published_fixture() => Assert.True(_result.Success, string.Join('\n', _result.Diagnostics.Select(diagnostic => diagnostic.Message)));
    [Fact] void should_infer_the_folded_facts_source_type() => _result.Value!.Model.Application.Modules.Single().Features.Single().Slices
        .Single(slice => slice.Name == "Publication").Specifications.Single().ThenEvents.Single().EventSource!.Type
        .ShouldEqual(SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Uuid));
}
