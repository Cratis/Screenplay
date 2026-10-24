// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspacePropertyAssertionProvenance.when_finding_consumers;

public class with_errors_outside_the_relevant_regions : given.a_partial_example_expansion
{
    void Establish()
    {
        CompilationDiagnostics = [Error(5), Error(8, "unrelated.play")];
        ExpansionDiagnostics = [Error(15), Error(18)];
    }

    void Because() => Consumers = Provenance.Consumers(Occurrence);

    [Fact] void should_report_known_empty_consumers() => Consumers.ShouldBeEmpty();
    [Fact] void should_not_report_unavailable_consumers() => Consumers.ShouldNotBeNull();
}
