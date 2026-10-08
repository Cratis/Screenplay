// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_ScopedDiagnostics;

public class when_selecting_an_imported_slice : Specification
{
    ScopedDiagnosticResult _result;
    Dictionary<string, string> _sources;

    void Establish() => _sources = new(StringComparer.Ordinal)
    {
        ["application.play"] = """
            concept Name : String
            module M
              feature F
                import "slices/*.play"
            """,
        ["slices/target.play"] = """
            slice StateChange Target
              event Added
                value Name
                broken
            """,
        ["slices/other.play"] = """
            slice StateChange Other
              event OtherEvent
                unrelated
            """
    };

    void Because() => _result = ScopedDiagnostics.Select(_sources, "M.F.Target")!;

    [Fact] void should_use_the_import_placement() => _result.DeclarationCount.ShouldEqual(2);
    [Fact] void should_include_the_imported_error() => _result.Diagnostics.Any(diagnostic => diagnostic.Message.Contains("'broken'", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_not_include_sibling_document_errors() => _result.Diagnostics.Any(diagnostic => diagnostic.Message.Contains("'unrelated'", StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_resolve_types_outside_the_scope() => _result.Diagnostics.Any(diagnostic => diagnostic.Message.Contains("Unknown type", StringComparison.Ordinal)).ShouldBeFalse();
}
