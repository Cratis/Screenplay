// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_protected_reads.with_several_refused_reads;

public class and_several_commands : given.a_protected_read_model
{
    Diagnostic[] _refusals;

    void Because() => _refusals = [.. Bind(Source.Replace("id String", "id Int", StringComparison.Ordinal).Replace(
            "    slice StateView Lookup",
            "      command Second\n        id Int identifier\n        reads View as other by id\n        produces Recorded\n          for id\n    slice StateView Lookup",
            StringComparison.Ordinal))
        .Diagnostics.Where(value => value.Code == DiagnosticCodes.UnprotectableDecisionRead)];

    [Fact] void should_report_a_refusal_in_every_command() => _refusals.Length.ShouldEqual(2);
    [Fact] void should_name_each_command() => _refusals.Select(value => value.Arguments["command"]).ShouldEqual([
        DiagnosticArgument.Declaration("command", ["M", "F", "Write", "Update"]),
        DiagnosticArgument.Declaration("command", ["M", "F", "Write", "Second"])]);
    [Fact] void should_name_each_read() => _refusals.Select(value => value.Arguments["read"]).ShouldEqual([DiagnosticArgument.Text("View"), DiagnosticArgument.Text("other")]);
}
