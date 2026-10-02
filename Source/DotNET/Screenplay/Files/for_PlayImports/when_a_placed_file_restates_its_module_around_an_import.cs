// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Files.for_PlayImports;

public class when_a_placed_file_restates_its_module_around_an_import : given.a_folder
{
    void Establish()
    {
        _documents["application.play"] = "module Ordering\n  import \"Orders.play\"";
        _documents["Orders.play"] = "module Ordering\n  feature Orders\n    import \"Slices/*.play\"";
        _documents["Slices/PlaceOrder.play"] = "slice StateChange PlaceOrder";
    }

    void Because() => Resolve("application.play");

    [Fact] void should_report_nothing() => _diagnostics.ShouldBeEmpty();
    [Fact] void should_place_the_slice_in_the_restated_module_s_feature() => PlacementOf("Slices/PlaceOrder.play").ShouldEqual(new PlayPlacement(["Ordering", "Orders"]));
}
