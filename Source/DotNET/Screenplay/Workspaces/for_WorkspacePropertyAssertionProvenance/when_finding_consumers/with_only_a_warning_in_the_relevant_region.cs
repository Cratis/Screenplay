// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Workspaces.for_WorkspacePropertyAssertionProvenance.when_finding_consumers;

public class with_only_a_warning_in_the_relevant_region : given.a_partial_example_expansion
{
    void Establish() => ExpansionDiagnostics = [Error(8) with { Severity = DiagnosticSeverity.Warning }];

    void Because() => Consumers = Provenance.Consumers(Occurrence);

    [Fact] void should_report_known_empty_consumers() => Consumers!.ShouldBeEmpty();
    [Fact] void should_not_report_unavailable_consumers() => Consumers.ShouldNotBeNull();
}
