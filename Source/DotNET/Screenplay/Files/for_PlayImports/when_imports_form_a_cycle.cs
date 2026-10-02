// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Files.for_PlayImports;

public class when_imports_form_a_cycle : given.a_folder
{
    void Establish()
    {
        _documents["application.play"] = "module M\n  import \"A.play\"";
        _documents["A.play"] = "feature A\n  import \"B.play\"";
        _documents["B.play"] = "feature B\n  import \"A.play\"";
    }

    void Because() => Resolve("application.play");

    [Fact] void should_report_the_cycle() => _diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.ImportCycle);
}
