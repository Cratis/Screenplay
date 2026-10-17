// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring;

public class when_moving_same_text_references_between_owners : given.a_reference_batch
{
    WorkspaceAuthoringResult[] _results = [];

    void Establish() => Configure(("application.play", Left + Right));
    void Because()
    {
        var changed = Left.Replace("    depends on Helper\n", string.Empty, StringComparison.Ordinal) + Right + "    depends on Helper\n";
        _results = [Propose(Workspace, WorkspaceAuthoringReferencePolicy.Safe, ("application.play", changed)), Propose(Workspace, WorkspaceAuthoringReferencePolicy.Draft, ("application.play", changed))];
    }

    [Fact] void should_refuse_safe_and_draft_moves() => _results.ShouldEachConformTo(result => !result.Accepted);
    [Fact] void should_name_the_document() => _results.ShouldEachConformTo(result => result.Conflicts.Single().Document == Workspace.Documents.Single().Id);
    [Fact] void should_name_the_path() => _results.ShouldEachConformTo(result => result.Conflicts.Single().Path == PortablePlayPath.Parse("application.play"));
    [Fact] void should_list_both_occurrences() => _results.ShouldEachConformTo(result => result.Conflicts.Single().Message.Contains("disappeared: 'Helper'", StringComparison.Ordinal) && result.Conflicts.Single().Message.Contains("introduced: 'Helper'", StringComparison.Ordinal));
    [Fact] void should_explain_the_ambiguity() => _results.ShouldEachConformTo(result => result.Conflicts.Single().Message.Contains("same text", StringComparison.Ordinal));
    [Fact] void should_keep_property_candidates_empty() => _results.ShouldEachConformTo(result => result.Conflicts.Single().Candidates.IsEmpty);
    [Fact] void should_locate_the_original_member() => _results.ShouldEachConformTo(result => result.Conflicts.Single().Message.Contains("application.play' at 4:5", StringComparison.Ordinal));
}
