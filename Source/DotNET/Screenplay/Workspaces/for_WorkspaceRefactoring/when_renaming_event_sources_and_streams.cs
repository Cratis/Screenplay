// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_renaming_event_sources_and_streams
{
    const string Source = """
        // Account remains in this comment.
        eventsource Account
          identifier String
          stream Profile
        eventsource Other
          identifier String
          stream Profile
        module M
          feature F
            slice StateChange S
              event E
              command C
                id String identifier
                stream Account.Profile
                produces E
                  for id
              command D
                id String identifier
                stream Other.Profile
                produces E
                  for id
              specification Routed
                given E
                  for "other"
                  stream Account.Profile
                when C
                  id = "other"
                then E
                  stream Account.Profile
              specification Appended
                when append E
                  for "other"
                  stream Account.Profile
                then E
                  no stream
        """;

    [Theory]
    [InlineData(WorkspaceAuthoringFormatting.PreserveTrivia)]
    [InlineData(WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments)]
    void should_rename_a_source_and_its_command_routes(WorkspaceAuthoringFormatting formatting)
    {
        var workspace = Workspace(Source);
        var result = Rename<EventSourceSyntax>(workspace, "Account", "Customer", formatting);
        Accepted(result);
        var nodes = Nodes(result);
        nodes.OfType<EventSourceSyntax>().Select(source => source.Name).ShouldContainOnly("Customer", "Other");
        nodes.OfType<CommandStreamSyntax>().Select(route => route.EventSource).ShouldContainOnly("Customer", "Other");
        nodes.OfType<EventStreamSyntax>().All(stream => stream.Id is null).ShouldBeTrue();
        nodes.OfType<EventSourceSyntax>().All(source => source.Id is null).ShouldBeTrue();
        CatalogContinuity(workspace, result.Workspace!);
        if (formatting == WorkspaceAuthoringFormatting.PreserveTrivia)
        {
            result.Workspace.Documents.Single().Text.ShouldEqual(Source.Replace("eventsource Account", "eventsource Customer", StringComparison.Ordinal).Replace("stream Account.Profile", "stream Customer.Profile", StringComparison.Ordinal));
        }
    }

    [Theory]
    [InlineData(WorkspaceAuthoringFormatting.PreserveTrivia)]
    [InlineData(WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments)]
    void should_rename_a_source_in_specification_routes(WorkspaceAuthoringFormatting formatting)
    {
        var result = Rename<EventSourceSyntax>(Workspace(Source), "Account", "Customer", formatting);
        Accepted(result);
        var nodes = Nodes(result);
        var routes = nodes.OfType<SpecificationStreamSyntax>().ToArray();
        routes.Length.ShouldEqual(3);
        routes.All(route => route.EventSource == "Customer" && route.Stream == "Profile").ShouldBeTrue();
        nodes.OfType<SpecificationNoStreamSyntax>().Count().ShouldEqual(1);
    }

    [Theory]
    [InlineData(WorkspaceAuthoringFormatting.PreserveTrivia)]
    [InlineData(WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments)]
    void should_rename_a_stream_only_under_its_source(WorkspaceAuthoringFormatting formatting)
    {
        var workspace = Workspace(Source);
        var result = Rename<EventStreamSyntax>(workspace, "Profile", "Details", formatting);
        Accepted(result);
        var nodes = Nodes(result);
        nodes.OfType<CommandStreamSyntax>().Single(route => route.EventSource == "Account").Stream.ShouldEqual("Details");
        nodes.OfType<CommandStreamSyntax>().Single(route => route.EventSource == "Other").Stream.ShouldEqual("Profile");
        nodes.OfType<EventSourceSyntax>().Single(source => source.Name == "Other").Streams.Single().Name.ShouldEqual("Profile");
        nodes.OfType<SpecificationStreamSyntax>().All(route => route.Stream == "Details").ShouldBeTrue();
        CatalogContinuity(workspace, result.Workspace!);
        if (formatting == WorkspaceAuthoringFormatting.PreserveTrivia)
        {
            result.Workspace.Documents.Single().Text.ShouldEqual(Source.Replace("eventsource Account\n  identifier String\n  stream Profile", "eventsource Account\n  identifier String\n  stream Details", StringComparison.Ordinal).Replace("stream Account.Profile", "stream Account.Details", StringComparison.Ordinal));
        }
    }

    [Theory]
    [InlineData(WorkspaceAuthoringFormatting.PreserveTrivia, false)]
    [InlineData(WorkspaceAuthoringFormatting.PreserveTrivia, true)]
    [InlineData(WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments, false)]
    [InlineData(WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments, true)]
    void should_patch_a_route_when_source_and_stream_share_a_name(WorkspaceAuthoringFormatting formatting, bool renameSource)
    {
        var source = Source.Replace("Profile", "Account", StringComparison.Ordinal);
        var workspace = Workspace(source);
        var result = renameSource
            ? Rename<EventSourceSyntax>(workspace, "Account", "Customer", formatting)
            : Rename<EventStreamSyntax>(workspace, "Account", "Details", formatting);
        Accepted(result);
        var nodes = Nodes(result);
        var command = nodes.OfType<CommandStreamSyntax>().Single(route => route.EventSource != "Other");
        command.EventSource.ShouldEqual(renameSource ? "Customer" : "Account");
        command.Stream.ShouldEqual(renameSource ? "Account" : "Details");
        nodes.OfType<SpecificationStreamSyntax>().All(route => route.EventSource == command.EventSource && route.Stream == command.Stream).ShouldBeTrue();
    }

    [Theory]
    [InlineData(WorkspaceAuthoringFormatting.PreserveTrivia, false)]
    [InlineData(WorkspaceAuthoringFormatting.PreserveTrivia, true)]
    [InlineData(WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments, false)]
    [InlineData(WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments, true)]
    void should_keep_pinned_stored_names(WorkspaceAuthoringFormatting formatting, bool eventNeverPersisted)
    {
        var source = Source.Replace("eventsource Account\n", "eventsource Account\n  id \"StoredAccount\"\n", StringComparison.Ordinal)
            .Replace("  stream Profile\n", "  stream Profile\n    id \"StoredProfile\"\n", StringComparison.Ordinal);
        var workspace = Workspace(source);
        var sourceResult = Rename<EventSourceSyntax>(workspace, "Account", "StoredAccount", formatting, eventNeverPersisted);
        Accepted(sourceResult);
        var streamResult = Rename<EventStreamSyntax>(sourceResult.Workspace!, "Profile", "StoredProfile", formatting, eventNeverPersisted);
        Accepted(streamResult);
        var nodes = Nodes(streamResult);
        nodes.OfType<EventSourceSyntax>().Single(item => item.Name == "StoredAccount").Id.ShouldEqual("StoredAccount");
        nodes.OfType<EventSourceSyntax>().Single(item => item.Name == "StoredAccount").Streams.Single().Id.ShouldEqual("StoredProfile");
        CatalogContinuity(workspace, streamResult.Workspace!);
        streamResult.Workspace.Documents.Single().Text.Split('\n').Where(line => line.TrimStart().StartsWith("id \"", StringComparison.Ordinal)).ShouldContainOnly([.. source.Split('\n').Where(line => line.TrimStart().StartsWith("id \"", StringComparison.Ordinal))]);
        streamResult.AuthoringDiagnostics.Any(diagnostic => diagnostic.Code == "PLAY0507").ShouldBeTrue();
    }

    [Theory]
    [InlineData(WorkspaceAuthoringFormatting.PreserveTrivia)]
    [InlineData(WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments)]
    void should_rename_across_files(WorkspaceAuthoringFormatting formatting)
    {
        var split = Source.IndexOf("module M", StringComparison.Ordinal);
        var workspace = Workspace(Source[..split], Source[split..]);
        var result = Rename<EventSourceSyntax>(workspace, "Account", "Customer", formatting);
        Accepted(result);
        result.WritePlan!.Entries.Length.ShouldEqual(2);
        Nodes(result).OfType<CommandStreamSyntax>().Single(route => route.Stream == "Profile" && route.EventSource != "Other").EventSource.ShouldEqual("Customer");
        Nodes(result).OfType<SpecificationStreamSyntax>().All(route => route.EventSource == "Customer").ShouldBeTrue();
    }

    [Theory]
    [InlineData(WorkspaceAuthoringFormatting.PreserveTrivia)]
    [InlineData(WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments)]
    void should_reject_a_source_name_collision(WorkspaceAuthoringFormatting formatting) =>
        Rejected(Rename<EventSourceSyntax>(Workspace(Source), "Account", "Other", formatting), "already declares event source 'Other'");

    [Theory]
    [InlineData(WorkspaceAuthoringFormatting.PreserveTrivia)]
    [InlineData(WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments)]
    void should_reject_a_sibling_stream_collision(WorkspaceAuthoringFormatting formatting) =>
        Rejected(Rename<EventStreamSyntax>(Workspace(Source.Replace("eventsource Account\n", "eventsource Account\n  stream Details\n", StringComparison.Ordinal)), "Profile", "Details", formatting), "already declares stream 'Details'");

    [Theory]
    [InlineData(WorkspaceAuthoringFormatting.PreserveTrivia, false)]
    [InlineData(WorkspaceAuthoringFormatting.PreserveTrivia, true)]
    [InlineData(WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments, false)]
    [InlineData(WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments, true)]
    void should_reject_a_duplicated_source(WorkspaceAuthoringFormatting formatting, bool renameStream)
    {
        var workspace = Workspace($"{Source}\neventsource Account\n  stream Extra");
        var result = renameStream
            ? Rename<EventStreamSyntax>(workspace, "Profile", "Details", formatting)
            : Rename<EventSourceSyntax>(workspace, "Account", "Customer", formatting);
        Rejected(result, "more than one physical declaration");
    }

    [Theory]
    [InlineData(WorkspaceAuthoringFormatting.PreserveTrivia, false)]
    [InlineData(WorkspaceAuthoringFormatting.PreserveTrivia, true)]
    [InlineData(WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments, false)]
    [InlineData(WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments, true)]
    void should_reject_capturing_route_debt(WorkspaceAuthoringFormatting formatting, bool renameStream)
    {
        var source = Source.Replace("stream Other.Profile", renameStream ? "stream Account.Details" : "stream Customer.Profile", StringComparison.Ordinal);
        var workspace = Workspace(source);
        var result = renameStream
            ? Rename<EventStreamSyntax>(workspace, "Profile", "Details", formatting)
            : Rename<EventSourceSyntax>(workspace, "Account", "Customer", formatting);
        Rejected(result, "binding");
    }

    [Theory]
    [InlineData(WorkspaceAuthoringFormatting.PreserveTrivia)]
    [InlineData(WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments)]
    void should_rename_sources_and_streams_in_production_routes_and_observer_filters(WorkspaceAuthoringFormatting formatting)
    {
        var source = Source.Replace("        produces E\n          for id\n      command D", "        produces E\n          for id\n          stream Account.Profile\n      command D", StringComparison.Ordinal) +
            "\n    slice Automation Follow\n      reaction R\n        from Account.Profile\n        when E\n      reducer Fold => Snapshot\n        from Account\n        on E\n          file Reducers/Fold.cs\n      readmodel Snapshot\n        id String key\n";
        var workspace = Workspace(source);
        var renamed = Rename<EventSourceSyntax>(workspace, "Account", "Customer", formatting);
        Accepted(renamed);
        var nodes = Nodes(renamed);
        nodes.OfType<ObserverFilterSyntax>().All(filter => filter.EventSource == "Customer").ShouldBeTrue();
        nodes.OfType<ProducesSyntax>().Where(produced => produced.Stream is not null).Single().Stream!.EventSource.ShouldEqual("Customer");
        var streamRename = Rename<EventStreamSyntax>(renamed.Workspace!, "Profile", "Details", formatting);
        Accepted(streamRename);
        Nodes(streamRename).OfType<ObserverFilterSyntax>().Single(filter => filter.Stream is not null).Stream.ShouldEqual("Details");
        CatalogContinuity(workspace, renamed.Workspace!);
    }

    static void CatalogContinuity(ScreenplayWorkspace before, ScreenplayWorkspace after)
    {
        // Names are catalog addresses, not stored-name pins: migrate addresses while preserving
        // every assigned ID, even when the pin keeps the stored route unchanged.
        Assert.NotEqual(before.IdentityCatalog.Revision, after.IdentityCatalog.Revision);
        after.IdentityCatalog.Semantics.Select(assignment => assignment.Id).ShouldContainOnly(before.IdentityCatalog.Semantics.Select(assignment => assignment.Id));
        after.IdentityCatalog.Documents.ShouldContainOnly(before.IdentityCatalog.Documents);
        after.IdentityCatalog.EventContracts.ShouldContainOnly(before.IdentityCatalog.EventContracts);
    }

    static ScreenplayWorkspace Workspace(params string[] sources)
    {
        var documents = sources.Select((source, index) => WorkspaceDocument.Create($"model{index}", PortablePlayPath.Parse($"model{index}.play"), Encoding.UTF8.GetBytes(source))).ToArray();
        var catalog = SemanticIdentityCatalog.Create(
            ApplicationIdentity.Create("A"),
            [.. documents.Select(document => new DocumentIdentityAssignment(document.StableKey, document.Id, SemanticIdentityOrigin.Persisted))],
            [],
            []);
        var workspace = ScreenplayWorkspace.Create("A", [.. documents], catalog);
        var entries = WorkspaceSyntaxIndex.Create(workspace).Entries;
        catalog = SemanticIdentityCatalog.PlanMigration(
            catalog,
            catalog.Revision,
            [.. documents.Select(document => document.StableKey)],
            [.. entries.Where(entry => entry.Address is not null).Select(entry => entry.Address!).Distinct()],
            [.. entries.Where(entry => entry.Node is EventSyntax).Select(entry => entry.Address!).Distinct()],
            [],
            [],
            []).Catalog;
        return ScreenplayWorkspace.Create("A", [.. documents], catalog);
    }

    static WorkspaceAuthoringResult Rename<T>(ScreenplayWorkspace workspace, string name, string newName, WorkspaceAuthoringFormatting formatting, bool eventNeverPersisted = false)
        where T : SyntaxNode => workspace.ProposeRename(new()
        {
            ExpectedRevision = workspace.Revision,
            ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
            Target = WorkspaceSyntaxIndex.Create(workspace).Entries.First(entry => entry.Node is T && WorkspaceReferenceBindings.Name(entry.Node) == name).Handle,
            ExpectedName = name,
            NewName = newName,
            Formatting = formatting,
            EventNeverPersisted = eventNeverPersisted
        });

    static SyntaxNode[] Nodes(WorkspaceAuthoringResult result) => [.. WorkspaceSyntaxIndex.Create(result.Workspace!).Entries.Select(entry => entry.Node)];

    static void Accepted(WorkspaceAuthoringResult result)
    {
        result.Conflicts.ShouldBeEmpty();
        result.Accepted.ShouldBeTrue();
    }

    static void Rejected(WorkspaceAuthoringResult result, string message)
    {
        result.Accepted.ShouldBeFalse();
        result.WritePlan.ShouldBeNull();
        result.Conflicts.Single().Message.ShouldContain(message);
    }
}
