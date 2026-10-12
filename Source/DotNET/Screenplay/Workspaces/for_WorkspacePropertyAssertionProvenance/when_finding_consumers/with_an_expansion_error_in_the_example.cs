// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspacePropertyAssertionProvenance.when_finding_consumers;

public class with_an_expansion_error_in_the_example : given.a_partial_example_expansion
{
    void Establish() => ExpansionDiagnostics = [Error(7)];

    void Because() => Consumers = Provenance.Consumers(Occurrence);

    [Fact] void should_report_unavailable_consumers() => Consumers.ShouldBeNull();
}
