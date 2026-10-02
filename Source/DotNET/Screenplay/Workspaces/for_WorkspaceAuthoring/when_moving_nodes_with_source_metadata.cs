// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring;

public class when_moving_nodes_with_source_metadata : Specification
{
    const string MovedEvent = "      event Moved // event intent\n        id \"Original\" // pin intent\n        value String // field intent\n";

    [Fact]
    void should_append_an_earlier_slice_to_a_later_feature()
    {
        var workspace = Create("module Projects\n  feature Registration\n    slice StateChange A // slice intent\n" + MovedEvent +
            "  feature Import\n    slice StateChange B\n      event Existing\n");
        var result = Move(workspace, entry => entry.Node is SliceSyntax slice && slice.Name == "A", entry => entry.Node is FeatureSyntax feature && feature.Name == "Import", "slices", false);
        var feature = WorkspaceSyntaxIndex.Create(result).Entries.Select(entry => entry.Node).OfType<FeatureSyntax>().Single(value => value.Name == "Import");
        feature.Slices.Select(slice => slice.Name).ShouldEqual(["B", "A"]);
    }

    [Fact]
    void should_move_the_first_event_to_the_end_of_its_slice()
    {
        var workspace = Create("module Projects\n  feature Registration\n    slice StateChange Register\n" + MovedEvent + "      event Existing\n");
        var result = Move(workspace, entry => entry.Node is EventSyntax declaration && declaration.Name == "Moved", entry => entry.Node is SliceSyntax, "events", false);
        WorkspaceSyntaxIndex.Create(result).Entries.Select(entry => entry.Node).OfType<SliceSyntax>().Single().Events.Select(value => value.Name).ShouldEqual(["Existing", "Moved"]);
    }

    [Fact]
    void should_keep_a_same_document_position_that_agrees_with_the_destination()
    {
        var workspace = Create("module Projects\n  feature Registration\n    slice StateChange Destination\n      event Existing\n      command BeforeMove\n    slice StateChange Source\n" + MovedEvent);
        var result = Move(workspace, entry => entry.Node is EventSyntax declaration && declaration.Name == "Moved", entry => entry.Node is SliceSyntax slice && slice.Name == "Destination", "events", true);
        var text = result.Documents[0].Text;
        text.IndexOf("command BeforeMove", StringComparison.Ordinal).ShouldBeLessThan(text.IndexOf("event Moved", StringComparison.Ordinal));
    }

    [Fact]
    void should_not_compare_positions_across_documents()
    {
        var workspace = Create("module Projects\n  feature Registration\n    slice StateChange Source\n" + MovedEvent,
            "module Projects\n  feature Registration\n    slice StateChange Destination\n      event Existing\n      command AfterEvents\n");
        var result = Move(workspace, entry => entry.Node is EventSyntax declaration && declaration.Name == "Moved", entry => entry.Node is SliceSyntax slice && slice.Name == "Destination", "events", false);
        var text = result.Documents.Single(document => document.StableKey == "document-1").Text;
        text.IndexOf("event Existing", StringComparison.Ordinal).ShouldBeLessThan(text.IndexOf("event Moved", StringComparison.Ordinal));
        text.IndexOf("event Moved", StringComparison.Ordinal).ShouldBeLessThan(text.IndexOf("command AfterEvents", StringComparison.Ordinal));
    }

    static ScreenplayWorkspace Move(ScreenplayWorkspace workspace, Func<WorkspaceSyntaxEntry, bool> subject, Func<WorkspaceSyntaxEntry, bool> destination, string member, bool keepsLocation)
    {
        var index = WorkspaceSyntaxIndex.Create(workspace);
        var target = index.Entries.Single(subject);
        var parent = index.Entries.Single(destination);
        var operation = new MoveWorkspaceNode(target.Handle, target.Node, parent.Handle, parent.Node, member);
        var edits = new WorkspaceAstEdits(index);
        edits.Prepare([operation]);
        var intended = edits.Apply()[parent.Handle.Document];
        var candidateEntries = WorkspaceSyntaxIndex.ForSyntax(intended, workspace.IdentityCatalog);
        var movedEntry = candidateEntries.Single(subject);
        var moved = movedEntry.Node;
        (moved.Location == target.Location).ShouldEqual(keepsLocation);
        moved.SourceComments.ShouldEqual(target.Node.SourceComments);
        moved.DirectiveLocations.ShouldEqual(target.Node.DirectiveLocations);

        var previousEvent = index.Entries.Single(entry => entry.Node is EventSyntax declaration && declaration.Name == "Moved").Address!;
        var currentEvent = candidateEntries.Single(entry => entry.Node is EventSyntax declaration && declaration.Name == "Moved").Address!;
        var renames = new List<SemanticIdentityRename>();
        if (!previousEvent.Equals(currentEvent))
        {
            renames.Add(new(previousEvent, currentEvent));
            renames.Add(new(SemanticAddress.ForProperty(previousEvent, "value"), SemanticAddress.ForProperty(currentEvent, "value")));
            if (target.Node is SliceSyntax) renames.Add(new(target.Address!, movedEntry.Address!));
        }

        var result = workspace.ProposeAuthoring(new()
        {
            ExpectedRevision = workspace.Revision,
            ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
            Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments,
            Validation = WorkspaceAuthoringValidation.Authoring,
            Operations = [operation],
            SemanticRenames = [.. renames],
            EventRenames = previousEvent.Equals(currentEvent) ? [] : [new(previousEvent, currentEvent)]
        });
        result.Conflicts.ShouldBeEmpty();
        Comments(result.Workspace!).ShouldEqual(Comments(workspace));
        WorkspaceSyntaxIndex.Create(result.Workspace!).Entries.Select(entry => entry.Node).OfType<EventSyntax>().Single(value => value.Name == "Moved").Id.ShouldEqual("Original");
        return result.Workspace!;
    }

    static ScreenplayWorkspace Create(params string[] sources) => ScreenplayWorkspace.Create("Projects",
        [.. sources.Select((source, position) => WorkspaceDocument.Create($"document-{position}", PortablePlayPath.Parse($"document-{position}.play"), Encoding.UTF8.GetBytes(source)))],
        SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects")));

    static string[] Comments(ScreenplayWorkspace workspace) => [.. workspace.Documents.SelectMany(document => WorkspaceSourceTokenizer.Tokenize(document).Tokens)
        .Where(token => token.Kind == WorkspaceSourceTokenKind.Comment).Select(token => token.Text).Order(StringComparer.Ordinal)];
}
