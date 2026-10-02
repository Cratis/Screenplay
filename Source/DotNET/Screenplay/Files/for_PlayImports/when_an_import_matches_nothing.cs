// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Files.for_PlayImports;

public class when_an_import_matches_nothing : given.a_folder
{
    void Establish() => _documents["application.play"] = "import \"Missing/**/*.play\"\nimport \"Missing.play\"";

    void Because() => Resolve("application.play");

    [Fact] void should_warn_about_the_pattern() => _diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.FileImportMatchesNothing && diagnostic.Severity == DiagnosticSeverity.Warning);
    [Fact] void should_fail_on_the_missing_file() => _diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.ImportedFileNotFound && diagnostic.Severity == DiagnosticSeverity.Error);
    [Fact] void should_report_where_the_import_is_written() => _diagnostics.All(diagnostic => diagnostic.Location.Path == "application.play").ShouldBeTrue();
}
