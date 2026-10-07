// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_ScopedDiagnostics;

public class when_a_document_contains_a_lone_carriage_return : Specification
{
    ScopedDiagnosticResult _result;
    readonly Dictionary<string, string> _sources = new(StringComparer.Ordinal)
    {
        ["application.play"] = "module M\n  feature F\n    slice StateChange Add\n      event Added",
        ["consumer.play"] = "// comment\rstill comment\nmodule N\n  feature F\n    slice StateChange Use\n      command Consume\n        id String identifier\n        produces Added\n          for id\n        broken\n      command Unrelated\n        unrelatedBroken"
    };

    void Because() => _result = ScopedDiagnostics.Select(_sources, "M.F.Add")!;

    [Fact] void should_include_the_direct_dependent_error_on_the_parser_line() => _result.Diagnostics.Any(diagnostic => diagnostic.Location.Path == "consumer.play" && diagnostic.Location.Line == 9).ShouldBeTrue();
    [Fact] void should_exclude_the_unrelated_command_error() => _result.Diagnostics.Any(diagnostic => diagnostic.Location.Path == "consumer.play" && diagnostic.Location.Line == 11).ShouldBeFalse();
}
