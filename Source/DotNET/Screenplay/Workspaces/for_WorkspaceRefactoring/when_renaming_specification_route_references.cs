// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_renaming_specification_route_references
{
    [Theory]
    [InlineData(WorkspaceAuthoringFormatting.PreserveTrivia)]
    [InlineData(WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments)]
    void should_repair_event_references_without_changing_occurrence_routes(WorkspaceAuthoringFormatting formatting)
    {
        const string Source = """
            eventsource Account
              identifier String
              stream Profile
                streamId String
            module M
              feature F
                slice StateChange S
                  event E
                    stream String
                    streamId String
                  command C
                    id String identifier
                    stream Account.Profile
                      streamId = "key"
                    produces E
                      for id
                      stream = "payload"
                      streamId = "payload"
                  command D
                    id String identifier
                    produces E
                      for id
                      stream = "payload"
                      streamId = "payload"
                  specification Routed
                    given E
                      for "other"
                      stream Account.Profile
                        streamId = "key"
                      stream = "payload"
                    when C
                      id = "other"
                    then E
                      stream Account.Profile
                        streamId = "key"
                      streamId = "payload"
                  specification Unrouted
                    when D
                      id = "other"
                    then E
                      no stream
            """;
        var document = WorkspaceDocument.Create("model", PortablePlayPath.Parse("model.play"), Encoding.UTF8.GetBytes(Source));
        var workspace = ScreenplayWorkspace.Create("A", [document], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("A")));
        var target = WorkspaceSyntaxIndex.Create(workspace).Entries.Single(entry => entry.Node is EventSyntax);
        var result = workspace.ProposeRename(new()
        {
            ExpectedRevision = workspace.Revision,
            ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
            Target = target.Handle,
            ExpectedName = "E",
            NewName = "Recorded",
            Formatting = formatting
        });
        result.Conflicts.ShouldBeEmpty();
        result.Accepted.ShouldBeTrue();
        var entries = WorkspaceSyntaxIndex.Create(result.Workspace!).Entries;
        var occurrences = entries.Select(entry => entry.Node).OfType<SpecificationEventSyntax>().ToArray();
        occurrences.Length.ShouldEqual(3);
        occurrences.All(occurrence => occurrence.EventType == "Recorded").ShouldBeTrue();
        var routes = entries.Select(entry => entry.Node).OfType<SpecificationStreamSyntax>().ToArray();
        routes.Length.ShouldEqual(2);
        routes.All(route => route.EventSource == "Account" && route.Stream == "Profile").ShouldBeTrue();
        routes.All(route => route.StreamId!.Source is LiteralExpressionSyntax { Value: string value } && value == "key").ShouldBeTrue();
        entries.Count(entry => entry.Node is SpecificationNoStreamSyntax).ShouldEqual(1);
        occurrences.SelectMany(occurrence => occurrence.Values).Select(mapping => mapping.Property).ShouldContainOnly("stream", "streamId");
    }
}
