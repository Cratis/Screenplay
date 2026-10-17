// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring;

public class when_batching_references_with_equal_tail_names : given.a_reference_batch
{
    WorkspaceAuthoringResult _result = null!;

    void Establish() => Configure(("application.play", Left.Replace("depends on Helper", "depends on Left.Helper", StringComparison.Ordinal) + Right));
    void Because() => _result = Propose(
        Workspace,
        WorkspaceAuthoringReferencePolicy.Safe,
        ("application.play", Left.Replace("    depends on Helper\n", string.Empty, StringComparison.Ordinal) + Right + "    depends on Right.Helper\n"));

    [Fact] void should_treat_differently_qualified_text_as_independent_edits() => _result.Accepted.ShouldBeTrue();
    [Fact] void should_not_report_debt() => _result.AuthoringDiagnostics.Any(diagnostic => diagnostic.Message.StartsWith("Reference debt:", StringComparison.Ordinal)).ShouldBeFalse();
}
