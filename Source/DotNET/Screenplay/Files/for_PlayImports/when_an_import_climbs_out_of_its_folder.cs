// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Files.for_PlayImports;

public class when_an_import_climbs_out_of_its_folder : given.a_folder
{
    void Establish()
    {
        _documents["Ordering/Ordering.play"] = "module Ordering\n  import \"../Shared/*.play\"";
        _documents["Shared/Concepts.play"] = "concept OrderId : Uuid";
        _documents["Shared/Nested/Deep.play"] = "concept Deep : Uuid";
    }

    void Because() => Resolve("Ordering/Ordering.play");

    [Fact] void should_import_the_matching_file() => PlacementOf("Shared/Concepts.play").Scope.ShouldContainOnly("Ordering");
    [Fact] void should_not_match_across_folders_with_a_single_star() => _resolved.Any(document => document.Path == "Shared/Nested/Deep.play").ShouldBeFalse();
}
