// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_ScopedDiagnostics;

public class when_selecting_an_ambiguous_scope : Specification
{
    ScopedDiagnosticResult? _result;
    string? _error;
    McpSnapshot _snapshot;

    void Establish() => _snapshot = new(new Dictionary<string, string>
    {
        ["application.play"] = "module M\n  feature F\n    slice StateChange Duplicate\n    slice StateChange Duplicate"
    });
    void Because() => _result = ScopedDiagnostics.Select(_snapshot, "M.F.Duplicate", out _error);

    [Fact] void should_refuse_to_merge_the_matching_nodes() => _result.ShouldBeNull();
    [Fact] void should_explain_the_ambiguity() => _error.ShouldContain("Ambiguous scope 'M.F.Duplicate'");
}
