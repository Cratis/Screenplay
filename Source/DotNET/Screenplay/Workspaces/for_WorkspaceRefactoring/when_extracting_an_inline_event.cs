// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_extracting_an_inline_event : Specification
{
    const string Source = "module Projects\n  feature Naming\n    slice StateChange Rename\n      command Rename\n        projectId Uuid identifier\n        name String\n        produces event Renamed // production intent\n          id \"OldName\" // identity intent\n          description \"A new name\" // description intent\n          documentation\n            ```markdown\n            Name history.\n            ```\n          tag audit\n          // mapping intent\n          name String = name // same comment\n";
    ScreenplayWorkspace _workspace;
    WorkspaceSyntaxEntry _subject;
    WorkspaceAuthoringResult _result;

    void Establish()
    {
        _workspace = ScreenplayWorkspace.Create("Projects", [WorkspaceDocument.Create("source", PortablePlayPath.Parse("source.play"), Encoding.UTF8.GetBytes(Source))], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects")));
        _subject = WorkspaceSyntaxIndex.Create(_workspace).Entries.Single(value => value.Node is EventSyntax);
    }

    void Because() => _result = WorkspaceEventRefactorings.ProposeExtractInlineEvent(_workspace, _subject.Handle, Request());

    [Fact] void should_accept() => _result.Conflicts.ShouldBeEmpty();
    [Fact] void should_use_one_transaction() => WorkspaceRepairVerification.TransactionCount(_workspace).ShouldEqual(1);
    [Fact] void should_preserve_canonical_bytes() => WorkspaceRepairVerification.SameModel(_workspace, _result.Workspace!).ShouldBeTrue();
    [Fact] void should_preserve_catalog() => _result.Workspace!.IdentityCatalog.Revision.ShouldEqual(_workspace.IdentityCatalog.Revision);
    [Fact] void should_insert_the_destination() => _result.Workspace!.Documents[0].Text.ShouldContain("for projectId");
    [Fact] void should_move_the_declaration() => WorkspaceSyntaxIndex.Create(_result.Workspace!).Entries.Single(value => value.Node is EventSyntax).Member.ShouldEqual("events");
    [Fact] void should_preserve_the_pin() => ((EventSyntax)WorkspaceSyntaxIndex.Create(_result.Workspace!).Entries.Single(value => value.Node is EventSyntax).Node).Id.ShouldEqual("OldName");
    [Fact] void should_preserve_documentation() => _result.Workspace!.Documents[0].Text.ShouldContain("Name history.");
    [Fact] void should_preserve_every_comment_once() => Comments(_result.Workspace!).ShouldEqual(Comments(_workspace));

    [Fact]
    void should_require_formatting_consent() => WorkspaceEventRefactorings.ProposeExtractInlineEvent(_workspace, _subject.Handle, Request() with { Formatting = WorkspaceAuthoringFormatting.PreserveTrivia }).Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.FormattingConsentRequired);

    [Fact]
    void should_reject_stale_revisions() => WorkspaceEventRefactorings.ProposeExtractInlineEvent(_workspace, _subject.Handle, Request() with { ExpectedRevision = WorkspaceRevision.Parse("wsrev1:" + new string('0', 64)) }).Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.StaleWorkspaceRevision);

    [Fact]
    void should_keep_an_existing_destination()
    {
        var document = _workspace.Documents[0];
        var workspace = ScreenplayWorkspace.Create("Projects", [WorkspaceDocument.Create(document.StableKey, document.Path, Encoding.UTF8.GetBytes(Source.Replace("          tag audit", "          for projectId\n          tag audit", StringComparison.Ordinal)))], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects")));
        var result = WorkspaceEventRefactorings.ProposeExtractInlineEvent(workspace, WorkspaceSyntaxIndex.Create(workspace).Entries.Single(value => value.Node is EventSyntax).Handle, Request() with { ExpectedRevision = workspace.Revision, ExpectedCatalogRevision = workspace.IdentityCatalog.Revision });
        result.Accepted.ShouldBeTrue();
        WorkspaceRepairVerification.SameModel(workspace, result.Workspace!).ShouldBeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_extract_in_authored_order_with_standalone_siblings(bool earlierSibling)
    {
        var source = Source + "      event Later\n        value String\n";
        if (earlierSibling)
        {
            source = source.Replace("      command Rename", "      event Earlier\n        value String\n      command Rename", StringComparison.Ordinal);
        }

        var workspace = ScreenplayWorkspace.Create("Projects", [WorkspaceDocument.Create("source", PortablePlayPath.Parse("source.play"), Encoding.UTF8.GetBytes(source))], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects")));
        var subject = WorkspaceSyntaxIndex.Create(workspace).Entries.Single(entry => entry.Node is EventSyntax declaration && declaration.Name == "Renamed");
        var request = Request() with { ExpectedRevision = workspace.Revision, ExpectedCatalogRevision = workspace.IdentityCatalog.Revision };
        var result = WorkspaceEventRefactorings.ProposeExtractInlineEvent(workspace, subject.Handle, request);
        result.Conflicts.ShouldBeEmpty();
        WorkspaceRepairVerification.TransactionCount(workspace).ShouldEqual(1);
        WorkspaceRepairVerification.SameModel(workspace, result.Workspace!).ShouldBeTrue();
        result.Workspace!.IdentityCatalog.Revision.ShouldEqual(workspace.IdentityCatalog.Revision);
        Comments(result.Workspace).ShouldEqual(Comments(workspace));
        var events = WorkspaceSyntaxIndex.Create(result.Workspace).Entries.Select(entry => entry.Node).OfType<SliceSyntax>().Single().Events;
        events.Select(@event => @event.Name).ToArray().ShouldEqual(earlierSibling ? ["Earlier", "Renamed", "Later"] : ["Renamed", "Later"]);
        WorkspaceEventRefactorings.ProposeExtractInlineEvent(workspace, subject.Handle, request).Workspace!.Documents[0].Bytes.AsSpan()
            .SequenceEqual(result.Workspace.Documents[0].Bytes.AsSpan()).ShouldBeTrue();
    }

    static string[] Comments(ScreenplayWorkspace workspace) => [.. workspace.Documents.SelectMany(document => WorkspaceSourceTokenizer.Tokenize(document).Tokens).Where(value => value.Kind == WorkspaceSourceTokenKind.Comment).Select(value => value.Text).Order(StringComparer.Ordinal)];

    WorkspaceAuthoringRequest Request() => new()
    {
        ExpectedRevision = _workspace.Revision,
        ExpectedCatalogRevision = _workspace.IdentityCatalog.Revision,
        Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments,
        Validation = WorkspaceAuthoringValidation.Authoring
    };
}
