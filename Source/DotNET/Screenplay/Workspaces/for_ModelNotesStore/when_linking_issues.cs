// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_ModelNotesStore;

public class when_linking_issues : Specification
{
    ApplicationIdentity _application;
    ModelNote _issue = null!;
    ModelNotesRead _read = null!;

    void Establish()
    {
        _application = ApplicationIdentity.Create("Library");
        _issue = Issue([new("Cratis/Library#12", "https://github.com/Cratis/Library/issues/12") { Tracker = "github" }, new("LIB-7") { Tracker = "jira" }]);
    }

    void Because() => _read = ModelNotesStore.ReadBytes(ModelNotesStore.Serialize(Document(_issue)), _application);

    [Fact] void should_read_the_link() => _read.Status.ShouldEqual("ok");
    [Fact] void should_keep_the_kind() => _read.Document!.Notes[0].Kind.ShouldEqual("issue");
    [Fact] void should_keep_the_tracker() => _read.Document!.Notes[0].Refs.Select(reference => reference.Tracker).ShouldContainOnly("github", "jira");
    [Fact] void should_keep_the_key() => _read.Document!.Notes[0].Refs[1].Id.ShouldEqual("LIB-7");
    [Fact] void should_write_the_tracker() => Encoding.UTF8.GetString(ModelNotesStore.Serialize(Document(_issue))).ShouldContain("\"tracker\":\"github\"");
    [Fact] void should_refuse_a_link_without_a_reference() => Refuses(Issue([]));
    [Fact] void should_refuse_a_reference_without_a_tracker() => Refuses(Issue([new("LIB-7")]));
    [Fact] void should_refuse_an_unknown_tracker() => Refuses(Issue([new("LIB-7") { Tracker = "trello" }]));

    ModelNote Issue(ImmutableArray<ModelNotesReference> refs) =>
        new(ModelNotesStore.NewId(), "issue", "Register a book", new(Application: true), null, "author", DateTimeOffset.UtcNow, refs, null);

    ModelNotesDocument Document(ModelNote note) => new(_application.ToString(), [note]);

    void Refuses(ModelNote note) => Catch.Exception(() => ModelNotesStore.Serialize(Document(note))).ShouldBeOfExactType<InvalidModelNotes>();
}
