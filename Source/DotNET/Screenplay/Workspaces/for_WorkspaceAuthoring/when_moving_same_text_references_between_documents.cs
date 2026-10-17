// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring;

public class when_moving_same_text_references_between_documents : given.a_reference_batch
{
    WorkspaceAuthoringResult[] _results = [];

    void Establish() => Configure(("left.play", Left), ("right.play", Right));
    void Because()
    {
        var changed = new[] { ("left.play", Left.Replace("    depends on Helper\n", string.Empty, StringComparison.Ordinal)), ("right.play", Right + "    depends on Helper\n") };
        _results = [Propose(Workspace, WorkspaceAuthoringReferencePolicy.Safe, changed), Propose(Workspace, WorkspaceAuthoringReferencePolicy.Draft, changed)];
    }

    [Fact] void should_refuse_safe_and_draft_moves() => _results.ShouldEachConformTo(result => !result.Accepted);
    [Fact] void should_name_the_original_document() => _results.ShouldEachConformTo(result => result.Conflicts.Single().Document == Workspace.Documents.Single(document => document.Path.Value == "left.play").Id);
    [Fact] void should_name_the_original_path() => _results.ShouldEachConformTo(result => result.Conflicts.Single().Path == PortablePlayPath.Parse("left.play"));
    [Fact] void should_locate_the_disappeared_reference() => _results.ShouldEachConformTo(result => result.Conflicts.Single().Message.Contains("disappeared: 'Helper' (target, Container) in 'left.play' at 4:5", StringComparison.Ordinal));
    [Fact] void should_locate_the_introduced_reference() => _results.ShouldEachConformTo(result => result.Conflicts.Single().Message.Contains("introduced: 'Helper' (target, Container) in 'right.play' at 4:5", StringComparison.Ordinal));
}
