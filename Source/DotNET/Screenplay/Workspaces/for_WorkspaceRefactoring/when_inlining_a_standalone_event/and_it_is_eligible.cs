// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_inlining_a_standalone_event;

public class and_it_is_eligible : given.a_standalone_event
{
    void Because() => _result = Inline();

    [Fact] void should_accept() => _result.Conflicts.ShouldBeEmpty();
    [Fact] void should_use_one_transaction() => WorkspaceRepairVerification.TransactionCount(_workspace).ShouldEqual(1);
    [Fact] void should_preserve_canonical_bytes() => WorkspaceRepairVerification.SameModel(_workspace, _result.Workspace!).ShouldBeTrue();
    [Fact] void should_preserve_the_catalog() => _result.Workspace!.IdentityCatalog.Revision.ShouldEqual(_workspace.IdentityCatalog.Revision);
    [Fact] void should_preserve_every_comment_once() => Comments(_result.Workspace!).ShouldEqual(Comments(_workspace));
    [Fact] void should_declare_the_event_inline() => _result.Workspace!.Documents[0].Text.ShouldContain("produces event Renamed");
    [Fact] void should_no_longer_declare_a_standalone_event() => WorkspaceSyntaxIndex.Create(_result.Workspace!).Entries.Single(value => value.Node is EventSyntax).Member.ShouldEqual("inlineEvent");
    [Fact] void should_preserve_the_pin() => ((EventSyntax)WorkspaceSyntaxIndex.Create(_result.Workspace!).Entries.Single(value => value.Node is EventSyntax).Node).Id.ShouldEqual("OldName");
    [Fact] void should_preserve_the_description_once() => _result.Workspace!.Documents[0].Text.Split("A new name").Length.ShouldEqual(2);
    [Fact] void should_preserve_the_documentation_once() => _result.Workspace!.Documents[0].Text.Split("Name history.").Length.ShouldEqual(2);
    [Fact] void should_keep_the_destination() => _result.Workspace!.Documents[0].Text.ShouldContain("for projectId");
    [Fact] void should_keep_the_semantic_identities() => Addresses(_result.Workspace!).ShouldContainOnly(Addresses(_workspace));

    static SemanticAddress[] Addresses(ScreenplayWorkspace workspace) => [.. WorkspaceSyntaxIndex.Create(workspace).Entries.Where(entry => entry.Address is not null).Select(entry => entry.Address!)];
}
