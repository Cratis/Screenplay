// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_ScopedDiagnostics;

public class when_an_imported_slice_has_a_stray_line : Specification
{
    ScopedDiagnosticResult _result;
    Dictionary<string, string> _sources;

    void Establish() => _sources = new()
    {
        ["application.play"] = "module M\n  feature F\n    import \"slice.play\"",
        ["slice.play"] = "import\nslice StateChange Clean\n  event Added\n    value String"
    };

    void Because() => _result = ScopedDiagnostics.Select(_sources, "M.F.Clean")!;

    [Fact] void should_include_the_error_before_any_declaration() => _result.Diagnostics.Any(diagnostic => diagnostic.Location.Path == "slice.play" && diagnostic.Location.Line == 1).ShouldBeTrue();
}
