// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_pinning_a_renamed_event : Specification
{
    const string Source = "module Projects\n  feature Naming\n    slice StateChange Rename\n      command Rename\n        projectId Uuid identifier\n        produces event Renamed // header intent\n          name   String = \"something\" // mapping intent\n";

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    void should_preserve_trivia_and_default_to_pinning(bool standalone, bool neverPersisted)
    {
        var source = standalone ? Source.Replace("produces event Renamed", "produces Renamed\n          for projectId\n      event Renamed", StringComparison.Ordinal)
            .Replace("name   String = \"something\"", "name   String", StringComparison.Ordinal) : Source;
        var workspace = Create(source);
        var result = Rename(workspace, "Renamed", "RenamedAgain", neverPersisted);
        result.Accepted.ShouldBeTrue();
        var changed = result.Workspace!;
        Event(changed).Id.ShouldEqual(neverPersisted ? null : "Renamed");
        changed.Documents[0].Text.ShouldContain("// header intent");
        changed.Documents[0].Text.ShouldContain("// mapping intent");
        changed.Documents[0].Text.ShouldContain("name   String");
        changed.IdentityCatalog.Semantics.Select(value => value.Id).OrderBy(value => value.ToString()).ShouldEqual(workspace.IdentityCatalog.Semantics.Select(value => value.Id).OrderBy(value => value.ToString()));
    }

    [Theory]
    [InlineData("\n")]
    [InlineData("\r\n")]
    void should_keep_a_pin_across_repeated_renames_and_remove_it_on_rename_back(string newline)
    {
        var source = Source.Replace("\n", newline, StringComparison.Ordinal);
        var once = Rename(Create(source), "Renamed", "First").Workspace!;
        var twice = Rename(once, "First", "Second", true).Workspace!;
        Event(twice).Id.ShouldEqual("Renamed");
        var back = Rename(twice, "Second", "Renamed");
        back.Accepted.ShouldBeTrue();
        Event(back.Workspace!).Id.ShouldBeNull();
        back.Workspace!.Documents[0].Bytes.ShouldEqual(Encoding.UTF8.GetBytes(source));
        back.AuthoringDiagnostics.Any(value => value.Code == DiagnosticCodes.RedundantEventId).ShouldBeFalse();
    }

    [Fact]
    void should_preserve_a_comment_on_a_removed_pin()
    {
        var workspace = Create(Source.Replace("event Renamed", "event First", StringComparison.Ordinal).Replace("          name", "          id \"Renamed\" // pin history\n          name", StringComparison.Ordinal));
        var result = Rename(workspace, "First", "Renamed");
        result.Accepted.ShouldBeTrue();
        result.Workspace!.Documents[0].Text.ShouldContain("// pin history");
    }

    [Theory]
    [InlineData("behavior Observe\n  on event Renamed\n    notify info \"Changed\"\n")]
    [InlineData("      screen Observe\n        on event Renamed\n          notify info \"Changed\"\n")]
    void should_rename_behavior_event_references_with_preserved_trivia(string consumer)
    {
        var result = Rename(Create(Source + consumer), "Renamed", "Again");
        result.Conflicts.ShouldBeEmpty();
        WorkspaceSyntaxIndex.Create(result.Workspace!).Entries.Select(entry => entry.Node).OfType<EventInteractionTriggerSyntax>()
            .Single().EventName.ShouldEqual("Again");
        result.Workspace!.Documents[0].Text.ShouldContain(consumer.Replace("Renamed", "Again", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_pin_generations_consistently_or_refuse_contradictory_pins(bool contradictory)
    {
        var source = "module Projects\n  feature Naming\n    slice StateChange Rename\n      event Renamed generation 1\n        name String\n      event Renamed generation 2\n        name String\n";
        if (contradictory) source += "        id \"Other\"\n";
        var result = Rename(Create(source), "Renamed", "Again");
        result.Accepted.ShouldEqual(!contradictory);
        if (!contradictory)
        {
            WorkspaceSyntaxIndex.Create(result.Workspace!).Entries.Select(value => value.Node).OfType<EventSyntax>().All(value => value.Id == "Renamed").ShouldBeTrue();
        }
    }

    static ScreenplayWorkspace Create(string source) => ScreenplayWorkspace.Create("Projects", [WorkspaceDocument.Create("source", PortablePlayPath.Parse("source.play"), Encoding.UTF8.GetBytes(source))], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects")));

    static EventSyntax Event(ScreenplayWorkspace workspace) => WorkspaceSyntaxIndex.Create(workspace).Entries.Select(value => value.Node).OfType<EventSyntax>().Single();

    static WorkspaceAuthoringResult Rename(ScreenplayWorkspace workspace, string before, string after, bool neverPersisted = false) => workspace.ProposeRename(new()
    {
        ExpectedRevision = workspace.Revision,
        ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
        Target = WorkspaceSyntaxIndex.Create(workspace).Entries.First(value => value.Node is EventSyntax).Handle,
        ExpectedName = before,
        NewName = after,
        EventNeverPersisted = neverPersisted
    });
}
