// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Files.for_PlayImports;

public class when_a_feature_file_sorts_before_the_module_that_imports_it : given.a_folder
{
    void Establish()
    {
        _documents["Catalog/A/A.play"] = "feature A\n  import \"*.play\"";
        _documents["Catalog/A/Register.play"] = "slice StateChange Register";
        _documents["Catalog/Catalog.play"] = "module Catalog\n  import \"A/A.play\"";
        _documents["application.play"] = "import \"Catalog/Catalog.play\"";
    }

    // Every file is a root when a folder is compiled, in path order - the feature file is met before its module.
    void Because() => Resolve("Catalog/A/A.play", "Catalog/A/Register.play", "Catalog/Catalog.play", "application.play");

    [Fact] void should_report_nothing() => _diagnostics.ShouldBeEmpty();
    [Fact] void should_place_the_feature_file_in_its_module() => PlacementOf("Catalog/A/A.play").Scope.ShouldContainOnly("Catalog");
    [Fact] void should_place_the_slice_file_in_its_feature() => PlacementOf("Catalog/A/Register.play").ShouldEqual(new PlayPlacement(["Catalog", "A"]));
}
