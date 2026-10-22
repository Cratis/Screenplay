// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator;

public class when_published_history_has_no_unambiguous_producer : given.a_published_history
{
    [Fact]
    void should_reject_an_unproduced_public_event()
    {
        var source = Source.Replace("from PriceChanged\n", "from OtherChanged\n", StringComparison.Ordinal)
            .Replace("price = price", "price = 10.5", StringComparison.Ordinal);
        var model = Compile(source);
        var module = model.Application.Modules.Single();
        var feature = module.Features.Single();
        var slices = feature.Slices.Select(slice => slice with { Commands = [.. slice.Commands.Where(command => command.Name != "ChangeOther")] }).ToImmutableArray();
        var application = model.Application with { Modules = [module with { Features = [feature with { Slices = slices }] }] };
        AssertRejected(Plan(ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, application)));
    }

    [Fact]
    void should_reject_ambiguous_private_source_types()
    {
        var source = Source.Replace("from PriceChanged\n", "from PriceChanged, OtherChanged\n", StringComparison.Ordinal)
            .Replace("price = price", "price = 10.5", StringComparison.Ordinal);
        AssertRejected(Plan(Compile(source)));
    }

    [Fact]
    void should_reject_a_mistyped_public_source()
    {
        var fact = _published with { Context = new(new(SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text), _published.Destination)) };
        var result = new SemanticEvaluator().EstablishWorld(_plan, [fact]);
        result.ShouldBeOfExactType<SemanticRejected>();
        ((SemanticRejected)result).Category.ShouldEqual(SemanticRejectionCategory.Contract);
    }

    void AssertRejected(SemanticExecutionPlan plan)
    {
        var result = new SemanticEvaluator().EstablishWorld(plan, [_published]);
        result.ShouldBeOfExactType<SemanticRejected>();
        ((SemanticRejected)result).Category.ShouldEqual(SemanticRejectionCategory.Contract);
        result.World.ShouldBeSame(SemanticWorld.Empty);
    }
}
