// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring;

public class when_batching_changed_text_with_draft_references : given.a_reference_batch
{
    WorkspaceAuthoringResult _batch = null!;
    WorkspaceAuthoringResult _sequential = null!;

    WorkspaceAuthoringResult _removal = null!;
    string _changed = null!;

    void Establish()
    {
        Configure(("application.play", Left + Right));
        var removed = Left.Replace("    depends on Helper\n", string.Empty, StringComparison.Ordinal) + Right;
        _changed = removed + "    depends on Missing\n";
        _removal = Propose(Workspace, WorkspaceAuthoringReferencePolicy.Draft, ("application.play", removed));
        _sequential = Propose(_removal.Workspace!, WorkspaceAuthoringReferencePolicy.Draft, ("application.play", _changed));
    }

    void Because() => _batch = Propose(Workspace, WorkspaceAuthoringReferencePolicy.Draft, ("application.play", _changed));

    [Fact] void should_accept_the_sequential_removal() => _removal.Accepted.ShouldBeTrue();
    [Fact] void should_accept_the_independent_edits() => _batch.Accepted.ShouldBeTrue();
    [Fact] void should_match_sequential_admission() => _batch.Accepted.ShouldEqual(_sequential.Accepted);
    [Fact] void should_record_the_same_debt_as_sequential_addition() => _batch.AuthoringDiagnostics.Where(diagnostic => diagnostic.Message.StartsWith("Reference debt:", StringComparison.Ordinal)).Select(diagnostic => diagnostic.Message).ShouldEqual(_sequential.AuthoringDiagnostics.Where(diagnostic => diagnostic.Message.StartsWith("Reference debt:", StringComparison.Ordinal)).Select(diagnostic => diagnostic.Message));
    [Fact] void should_record_the_missing_target() => _batch.AuthoringDiagnostics.Any(diagnostic => diagnostic.Message.Contains("Reference debt: unresolved Container 'Missing'", StringComparison.Ordinal)).ShouldBeTrue();
}
