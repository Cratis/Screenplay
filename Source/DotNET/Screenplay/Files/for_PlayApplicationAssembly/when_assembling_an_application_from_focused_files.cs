// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Files.for_PlayApplicationAssembly;

public class when_assembling_an_application_from_focused_files : Specification
{
    readonly Dictionary<string, string> _documents = new(StringComparer.Ordinal)
    {
        ["application.play"] = "concept OrderId : Uuid\nimport \"**/*.play\"",
        ["Ordering/Ordering.play"] = "module Ordering\n  description \"Orders\"\n  authorize Staff\n  import \"*/*.play\"",
        ["Ordering/Orders/Orders.play"] = "feature Orders\n  description \"Placing orders\"\n  import \"*.play\"",
        ["Ordering/Orders/PlaceOrder.play"] = "slice StateChange PlaceOrder\n  command PlaceOrder\n    orderId OrderId identifier\n    produces OrderPlaced\n      for orderId\n  event OrderPlaced\n    note String",
        ["Ordering/Orders/CancelOrder.play"] = "slice StateChange CancelOrder\n  command CancelOrder\n    orderId OrderId identifier\n    produces OrderCancelled\n      for orderId\n  event OrderCancelled\n    reason String",
        ["Access.play"] = "policy Staff\n  require authenticated"
    };

    IReadOnlyList<PlacedPlayDocument> _documentsAssembled;
    CompilationResult<ApplicationSyntax> _result;

    void Because() => (_documentsAssembled, _result) = PlayApplicationAssembly.Compile(new ScreenplayCompiler(), ["application.play"], new InMemoryPlayDocumentSource(_documents));

    [Fact] void should_compile_without_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_assemble_every_file() => _documentsAssembled.Count.ShouldEqual(6);
    [Fact] void should_merge_into_one_module() => _result.Value!.Modules.Single().Name.ShouldEqual("Ordering");
    [Fact] void should_keep_the_module_description() => Module.Description.ShouldEqual("Orders");
    [Fact] void should_merge_into_one_feature() => Feature.Name.ShouldEqual("Orders");
    [Fact] void should_keep_the_feature_description() => Feature.Description.ShouldEqual("Placing orders");
    [Fact] void should_put_both_slices_in_the_feature() => Feature.Slices.Select(slice => slice.Name).ShouldContainOnly("CancelOrder", "PlaceOrder");
    [Fact] void should_no_longer_mark_the_module_as_a_placement() => Module.IsPlacement.ShouldBeFalse();
    [Fact] void should_no_longer_mark_the_feature_as_a_placement() => Feature.IsPlacement.ShouldBeFalse();
    [Fact] void should_locate_the_module_at_its_declaration() => Module.Location.Path.ShouldEqual("Ordering/Ordering.play");

    ModuleSyntax Module => _result.Value!.Modules.Single();
    FeatureSyntax Feature => Module.Features.Single();
}
