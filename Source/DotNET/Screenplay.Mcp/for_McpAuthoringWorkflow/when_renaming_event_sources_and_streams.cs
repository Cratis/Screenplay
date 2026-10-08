// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_renaming_event_sources_and_streams : given.an_authoring_connection
{
    const string Source = """
        eventsource Account
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
              specification Routed
                given E
                  for "other"
                  stream Account.Profile
                when C
                  id = "other"
                then E
                  stream Account.Profile
        """;

    [Theory]
    [InlineData("EventSourceSyntax", "Account", "Customer", "Customer", "Profile")]
    [InlineData("EventStreamSyntax", "Profile", "Details", "Account", "Details")]
    public void should_propose_route_repairs_without_changing_disk(string kind, string name, string newName, string expectedSource, string expectedStream)
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), Source);
        Initialize();
        var opened = Open();
        var revision = opened.GetProperty("revision").GetString()!;
        var response = Call("propose-rename", new
        {
            expectedRevision = revision,
            expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
            target = Node(kind, revision).GetProperty("handle"),
            expectedName = name,
            newName
        });
        var proposal = response.GetProperty("result").GetProperty("structuredContent");
        File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldEqual(Source);
        var candidate = Candidate(proposal);
        var nodes = WorkspaceSyntaxIndex.Create(candidate).Entries.Select(entry => entry.Node).ToArray();
        var route = nodes.OfType<CommandStreamSyntax>().Single();
        route.EventSource.ShouldEqual(expectedSource);
        route.Stream.ShouldEqual(expectedStream);
        nodes.OfType<SpecificationStreamSyntax>().All(item => item.EventSource == expectedSource && item.Stream == expectedStream).ShouldBeTrue();
        nodes.OfType<EventSourceSyntax>().Single().Id.ShouldBeNull();
        nodes.OfType<EventStreamSyntax>().Single().Id.ShouldBeNull();
        File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldEqual(Source);
        File.Exists(Path.Combine(RootPath, ".screenplay", "identities.json")).ShouldBeFalse();
    }
}
