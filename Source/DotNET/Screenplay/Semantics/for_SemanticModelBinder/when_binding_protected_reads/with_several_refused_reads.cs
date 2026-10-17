// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_protected_reads;

public class with_several_refused_reads : given.a_protected_read_model
{
    const string SecondRead = "reads View as other by id";

    [Fact]
    void should_report_every_refused_read_of_one_command()
    {
        var source = Source.Replace("reads View by id", $"reads View as first by id\n        {SecondRead}", StringComparison.Ordinal)
            .Replace("View.value", "first.value", StringComparison.Ordinal).Replace("id String", "id Int", StringComparison.Ordinal);
        var refusals = Bind(source).Diagnostics.Where(value => value.Code == DiagnosticCodes.UnprotectableDecisionRead).ToArray();
        refusals.Length.ShouldEqual(2);
        refusals.Select(value => value.Arguments["read"]).ShouldEqual([DiagnosticArgument.Text("first"), DiagnosticArgument.Text("other")]);
        refusals.Select(value => value.Message).ShouldEqual(["Read 'View' cannot be protected: KeyType.", "Read 'View' cannot be protected: KeyType."]);
        refusals[0].Location.ShouldNotEqual(refusals[1].Location);
    }

    [Fact]
    void should_report_a_refused_read_in_every_command()
    {
        var source = Source.Replace("id String", "id Int", StringComparison.Ordinal).Replace(
            "    slice StateView Lookup",
            "      command Second\n        id Int identifier\n        reads View as other by id\n        produces Recorded\n          for id\n    slice StateView Lookup",
            StringComparison.Ordinal);
        var refusals = Bind(source).Diagnostics.Where(value => value.Code == DiagnosticCodes.UnprotectableDecisionRead).ToArray();
        refusals.Length.ShouldEqual(2);
        refusals.Select(value => value.Arguments["command"]).ShouldEqual([
            DiagnosticArgument.Declaration("command", ["M", "F", "Write", "Update"]),
            DiagnosticArgument.Declaration("command", ["M", "F", "Write", "Second"])]);
        refusals.Select(value => value.Arguments["read"]).ShouldEqual([DiagnosticArgument.Text("View"), DiagnosticArgument.Text("other")]);
    }
}
