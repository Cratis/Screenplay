// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Files.for_PlayImports;

public class when_two_imports_place_a_file_apart : given.a_folder
{
    void Establish()
    {
        _documents["application.play"] = "module Ordering\n  import \"Shared.play\"\nmodule Billing\n  import \"Shared.play\"";
        _documents["Shared.play"] = "feature Common";
    }

    void Because() => Resolve("application.play");

    [Fact] void should_report_the_conflict() => _diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.ConflictingImportPlacement);
    [Fact] void should_keep_the_first_placement() => PlacementOf("Shared.play").Scope.ShouldContainOnly("Ordering");
}
