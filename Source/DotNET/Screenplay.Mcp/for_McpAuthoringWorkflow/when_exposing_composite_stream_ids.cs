// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_exposing_composite_stream_ids : given.an_authoring_connection
{
    const string Source = "eventsource A\n  stream S\n    streamId\n      one String\n      two Uuid\n";

    [Fact]
    void should_expose_declared_parts_in_order_with_execution()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), Source);
        Initialize();
        var revision = Open().GetProperty("revision").GetString()!;
        var stream = Page("event-streams", revision)[0];
        var parts = stream.GetProperty("streamIdParts");
        parts[0].GetProperty("name").GetString().ShouldEqual("one");
        parts[1].GetProperty("name").GetString().ShouldEqual("two");
        parts[1].GetProperty("type").GetProperty("name").GetString().ShouldEqual("Uuid");
        stream.GetProperty("executionAvailable").GetBoolean().ShouldBeTrue();
        var details = Result("declaration-details", new { address = "A.S", kind = "EventStream" });
        details.GetRawText().ShouldContain("streamIdParts");
    }

    [Fact]
    void should_add_a_part_through_the_typed_collection_and_retain_admission()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), Source);
        Initialize();
        var opened = Open();
        var stream = Node("EventStreamSyntax", opened.GetProperty("revision").GetString()!);
        var proposal = Result("propose-ast", new
        {
            expectedRevision = opened.GetProperty("revision").GetString(),
            expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
            validation = "Authoring", formatting = "CanonicalizeTouchedDocuments",
            operations = new[] { new { operation = "add", parent = stream.GetProperty("handle"), member = "streamIdParts", node = new { kind = "EventStreamIdPartSyntax", name = "three", type = new { kind = "TypeRefSyntax", name = "String", isOptional = false, isCollection = false } } } }
        });
        proposal.GetProperty("success").GetBoolean().ShouldBeTrue();
        var candidate = Candidate(proposal);
        WorkspaceSyntaxIndex.Create(candidate).Entries.Select(entry => entry.Node).OfType<EventStreamIdPartSyntax>().Count().ShouldEqual(3);
        candidate.Compilation.Success.ShouldBeTrue();
        candidate.Compilation.Value!.Model.Application.EventSources.Single().Streams.Single().StreamIdParts.Select(part => part.Name).ShouldEqual(new[] { "one", "two", "three" });
    }
}
