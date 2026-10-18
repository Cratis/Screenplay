// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_renaming_a_stream_id_part : given.an_authoring_connection
{
    const string Source = """
        eventsource Account
          identifier String
          stream Ledger
            streamId
              account String
              period String
        module M
          feature F
            slice StateChange S
              event Recorded
              command Record
                account String identifier
                period String
                stream Account.Ledger
                  streamId
                    period = period
                    account = account
                produces Recorded
                  for account
              specification Routed
                when Record
                  account = "a"
                  period = "p"
                then Recorded
                  stream Account.Ledger
                    streamId
                      account = "a"
                      period = "p"
        """;

    [Theory]
    [InlineData(WorkspaceAuthoringFormatting.PreserveTrivia)]
    [InlineData(WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments)]
    public void should_propose_a_part_rename_without_writing_source_or_catalog(WorkspaceAuthoringFormatting formatting)
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), Source);
        Initialize();
        var opened = Open();
        var revision = opened.GetProperty("revision").GetString();
        var response = Call("propose-rename", new
        {
            expectedRevision = revision,
            expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
            target = Result("read-ast", new { expectedRevision = revision, kind = "EventStreamIdPartSyntax", includeContent = true })
                .GetProperty("page").GetProperty("items").EnumerateArray().Single(item => item.GetProperty("node").GetProperty("name").GetString() == "period").GetProperty("handle"),
            expectedName = "period",
            newName = "month",
            formatting = formatting.ToString()
        });
        var candidate = Candidate(response.GetProperty("result").GetProperty("structuredContent"));
        var nodes = WorkspaceSyntaxIndex.Create(candidate).Entries.Select(entry => entry.Node).ToArray();
        nodes.OfType<EventStreamSyntax>().Single().StreamIdParts.Select(part => part.Name).ShouldEqual(["account", "month"]);
        nodes.OfType<CommandStreamSyntax>().Single().StreamIdParts.Select(part => part.Property).ShouldEqual(["month", "account"]);
        nodes.OfType<SpecificationStreamSyntax>().Single().StreamIdParts.Select(part => part.Property).ShouldEqual(["account", "month"]);
        File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldEqual(Source);
        File.Exists(Path.Combine(RootPath, ".screenplay", "identities.json")).ShouldBeFalse();
    }
}
