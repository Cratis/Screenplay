// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Workspaces.for_WorkspacePropertyAssertionProvenance.when_finding_consumers;

public class with_a_locationless_error : given.a_partial_example_expansion
{
    void Establish() => ExpansionDiagnostics = [Diagnostic.Error(DiagnosticCodes.UnresolvedSpecificationExampleType, "Example expansion is incomplete.", new(0, 0))];

    void Because() => Consumers = Provenance.Consumers(Occurrence);

    [Fact] void should_report_unavailable_consumers() => Consumers.ShouldBeNull();
}
