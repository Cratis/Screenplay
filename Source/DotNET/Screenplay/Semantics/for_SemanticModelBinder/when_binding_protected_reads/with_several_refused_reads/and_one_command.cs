// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_protected_reads.with_several_refused_reads;

public class and_one_command : given.a_protected_read_model
{
    Diagnostic[] _refusals;

    void Because() => _refusals = [.. Bind(Source.Replace("reads View by id", "reads View as first by id\n        reads View as other by id", StringComparison.Ordinal)
            .Replace("View.value", "first.value", StringComparison.Ordinal).Replace("id String", "id Int", StringComparison.Ordinal))
        .Diagnostics.Where(value => value.Code == DiagnosticCodes.UnprotectableDecisionRead)];

    [Fact] void should_report_every_refused_read() => _refusals.Length.ShouldEqual(2);
    [Fact] void should_name_each_read() => _refusals.Select(value => value.Arguments["read"]).ShouldEqual([DiagnosticArgument.Text("first"), DiagnosticArgument.Text("other")]);
    [Fact] void should_keep_the_message_wording() => _refusals.Select(value => value.Message).ShouldEqual(["Read 'View' cannot be protected: KeyType.", "Read 'View' cannot be protected: KeyType."]);
    [Fact] void should_locate_each_read_separately() => _refusals[0].Location.ShouldNotEqual(_refusals[1].Location);
}
