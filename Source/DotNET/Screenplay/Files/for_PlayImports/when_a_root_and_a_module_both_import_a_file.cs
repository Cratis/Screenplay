// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Files.for_PlayImports;

public class when_a_root_and_a_module_both_import_a_file : given.a_folder
{
    void Establish()
    {
        _documents["application.play"] = "import \"**/*.play\"";
        _documents["Ordering/Ordering.play"] = "module Ordering\n  import \"**/*.play\"";
        _documents["Ordering/Orders/Orders.play"] = "feature Orders\n  import \"*.play\"";
        _documents["Ordering/Orders/PlaceOrder.play"] = "slice StateChange PlaceOrder";
    }

    void Because() => Resolve("application.play");

    [Fact] void should_resolve_every_file_once() => _resolved.Select(document => document.Path).ShouldContainOnly("application.play", "Ordering/Ordering.play", "Ordering/Orders/Orders.play", "Ordering/Orders/PlaceOrder.play");
    [Fact] void should_keep_the_root_a_whole_document() => PlacementOf("application.play").IsDocument.ShouldBeTrue();
    [Fact] void should_keep_the_module_file_a_whole_document() => PlacementOf("Ordering/Ordering.play").IsDocument.ShouldBeTrue();
    [Fact] void should_place_the_feature_file_in_the_module() => PlacementOf("Ordering/Orders/Orders.play").Scope.ShouldContainOnly("Ordering");
    [Fact] void should_place_the_slice_file_at_its_deepest_import() => PlacementOf("Ordering/Orders/PlaceOrder.play").ShouldEqual(new PlayPlacement(["Ordering", "Orders"]));
    [Fact] void should_report_nothing() => _diagnostics.ShouldBeEmpty();
}
