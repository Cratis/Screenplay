// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring;

public class when_batching_changed_text_with_safe_references : given.a_reference_batch
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
        _removal = Propose(Workspace, WorkspaceAuthoringReferencePolicy.Safe, ("application.play", removed));
        _sequential = Propose(_removal.Workspace!, WorkspaceAuthoringReferencePolicy.Safe, ("application.play", _changed));
    }

    void Because() => _batch = Propose(Workspace, WorkspaceAuthoringReferencePolicy.Safe, ("application.play", _changed));

    [Fact] void should_accept_the_sequential_removal() => _removal.Accepted.ShouldBeTrue();
    [Fact] void should_refuse_the_new_unresolved_reference() => _batch.Accepted.ShouldBeFalse();
    [Fact] void should_explain_the_new_debt() => _batch.Conflicts.Single().Message.ShouldContain("New unresolved Container reference 'Missing'");
    [Fact] void should_match_sequential_admission() => _batch.Accepted.ShouldEqual(_sequential.Accepted);
    [Fact] void should_name_the_document() => _batch.Conflicts.Single().Document.ShouldEqual(Workspace.Documents.Single().Id);
    [Fact] void should_name_the_path() => _batch.Conflicts.Single().Path.ShouldEqual(PortablePlayPath.Parse("application.play"));
}
