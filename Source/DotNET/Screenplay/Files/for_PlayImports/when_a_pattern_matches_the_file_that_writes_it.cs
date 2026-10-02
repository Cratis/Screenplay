// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Files.for_PlayImports;

public class when_a_pattern_matches_the_file_that_writes_it : given.a_folder
{
    void Establish()
    {
        _documents["Ordering/Ordering.play"] = "module Ordering\n  import \"*.play\"";
        _documents["Ordering/Orders.play"] = "feature Orders";
    }

    void Because() => Resolve("Ordering/Ordering.play");

    [Fact] void should_not_import_itself() => PlacementOf("Ordering/Ordering.play").IsDocument.ShouldBeTrue();
    [Fact] void should_import_its_siblings() => PlacementOf("Ordering/Orders.play").Scope.ShouldContainOnly("Ordering");
    [Fact] void should_report_nothing() => _diagnostics.ShouldBeEmpty();
}
