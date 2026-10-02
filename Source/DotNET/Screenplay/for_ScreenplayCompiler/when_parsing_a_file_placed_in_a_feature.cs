// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Files;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_parsing_a_file_placed_in_a_feature : given.a_compiler
{
    const string Source =
        """
        concept OrderId : Uuid

        description "Placing orders"

        slice StateChange PlaceOrder
          command PlaceOrder
            orderId OrderId identifier
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Parse(Source, "PlaceOrder.play", new PlayPlacement(["Ordering", "Orders"]));

    [Fact] void should_parse_without_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_keep_application_declarations_at_the_application() => _result.Value!.Concepts.Single().Name.ShouldEqual("OrderId");
    [Fact] void should_place_the_module() => Module.Name.ShouldEqual("Ordering");
    [Fact] void should_mark_the_module_as_a_placement() => Module.IsPlacement.ShouldBeTrue();
    [Fact] void should_mark_the_feature_as_a_placement() => Feature.IsPlacement.ShouldBeTrue();
    [Fact] void should_put_the_slice_in_the_feature() => Feature.Slices.Single().Name.ShouldEqual("PlaceOrder");
    [Fact] void should_give_the_feature_the_description() => Feature.Description.ShouldEqual("Placing orders");

    ModuleSyntax Module => _result.Value!.Modules.Single();
    FeatureSyntax Feature => Module.Features.Single();
}
