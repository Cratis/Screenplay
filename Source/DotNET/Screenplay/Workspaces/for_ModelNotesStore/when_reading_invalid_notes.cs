// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_ModelNotesStore;

public class when_reading_invalid_notes : Specification
{
    ApplicationIdentity _application;
    byte[] _valid = null!;

    void Establish()
    {
        _application = ApplicationIdentity.Create("Library");
        _valid = ModelNotesStore.Serialize(new(_application.ToString(), [new(ModelNotesStore.NewId(), "question", "Why?", new(Application: true), null, "author", DateTimeOffset.UtcNow, [], null)]));
    }

    [Fact] void should_report_malformed_as_unreadable() => Read("{").Status.ShouldEqual("unreadable");
    [Fact] void should_report_conflict_markers_as_unreadable() => Read("<<<<<<< ours\n{}\n=======\n{}\n>>>>>>> theirs").Status.ShouldEqual("unreadable");
    [Fact] void should_report_wrong_schema_as_unreadable() => Read(Encoding.UTF8.GetString(_valid).Replace("cratis.screenplay.notes", "wrong", StringComparison.Ordinal)).Status.ShouldEqual("unreadable");
    [Fact] void should_report_other_application_as_unreadable() => ModelNotesStore.ReadBytes(_valid, ApplicationIdentity.Create("Other")).Status.ShouldEqual("unreadable");
    [Fact] void should_report_invalid_kind_as_unreadable() => Read(Encoding.UTF8.GetString(_valid).Replace("question", "unknown", StringComparison.Ordinal)).Status.ShouldEqual("unreadable");
    [Fact] void should_distinguish_absence() => ModelNotesStore.ReadBytes(null, _application).Revision.ShouldEqual(ModelNotesStore.AbsentRevision);

    [Fact]
    void should_reject_duplicate_ids()
    {
        var note = ModelNotesStore.ReadBytes(_valid, _application).Document!.Notes[0];
        var json = System.Text.Json.JsonSerializer.Serialize(note, ModelNotesStore.Options);
        var contents = $"{{\"schema\":\"cratis.screenplay.notes\",\"schemaVersion\":1,\"applicationIdentity\":\"{_application}\",\"notes\":[{json},{json}]}}";
        Read(contents).Status.ShouldEqual("unreadable");
    }

    ModelNotesRead Read(string text) => ModelNotesStore.ReadBytes(Encoding.UTF8.GetBytes(text), _application);
}
