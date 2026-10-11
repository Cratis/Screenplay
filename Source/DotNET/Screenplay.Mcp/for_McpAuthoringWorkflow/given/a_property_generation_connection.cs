// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow.given;

public class a_property_generation_connection : an_authoring_connection
{
    internal const string Fanout = """
        module Projects
          feature Registration
            slice StateChange Register
              command Register
                id String identifier
                name String
                produces Registered
                  for id
                  name = name
                produces Audited
                  for id
                  name = name
              event Registered
                name String
              event Audited generation 1
                name String
              event Audited generation 2
                name String
        """;

    protected JsonElement Proposal;
    protected JsonElement Review;
    protected JsonElement Diff;
    protected JsonElement Applied;
    protected ScreenplayWorkspace Before;
    protected ScreenplayWorkspace After;
    protected ScreenplayWorkspace Reopened;
    protected byte[] Reviewed;

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), Fanout, new UTF8Encoding(false));
        Initialize();
    }

    protected void Evolve(int count, bool advance)
    {
        var opened = Open();
        Before = Workspace();
        var revision = opened.GetProperty("revision").GetString();
        var nodes = Result("read-ast", new { expectedRevision = revision, kind = "PropertySyntax", name = "name" }).GetProperty("page").GetProperty("items").EnumerateArray().ToArray();
        var target = nodes.Single(node => node.GetProperty("handle").GetProperty("path").GetString().Contains("/commands/", StringComparison.Ordinal)).GetProperty("handle");
        var registered = nodes.Single(node => NamesOwner(node, "Registered"));
        var audited = nodes.Single(node => NamesOwner(node, "Audited") && node.GetProperty("address").GetProperty("parts").EnumerateArray().Any(part => part.GetProperty("kind").GetString() == "Generation" && part.GetProperty("key").GetString() == "2"));
        // Reverse canonical event order and repeat one owner to exercise the public report's ordering and deduplication.
        var selected = count == 1 ? new[] { registered } : new[] { registered, audited, registered };
        Proposal = Result("propose-property", new
        {
            expectedRevision = revision,
            expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
            action = "rename", target, expectedName = "name", newName = "label",
            alsoRename = selected.Select(node => new { target = node.GetProperty("handle"), expectedName = "name" }),
            newGeneration = advance, eventNeverPersisted = !advance,
            formatting = "CanonicalizeTouchedDocuments"
        });
        After = Candidate(Proposal);
        var id = Proposal.GetProperty("proposalId").GetString();
        Review = Result("read-proposal", new { proposalId = id });
        Diff = Result("read-proposal", new { proposalId = id, view = "semantic-diff" });
        Reviewed = Result("read-proposal", new { proposalId = id, view = "after", documentId = target.GetProperty("documentId").GetString() }).GetProperty("result").GetProperty("content").GetProperty("bytesBase64").GetBytesFromBase64();
        Applied = Apply(opened, Proposal);
        Connection = new(new McpTools(new McpRoot(RootPath)));
        Initialize();
        _ = Open();
        var state = File.ReadAllBytes(Path.Combine(RootPath, ".screenplay", "identities.json"));
        Reopened = McpState.Deserialize(state).Open(Root);
    }

    static bool NamesOwner(JsonElement node, string owner) => node.GetProperty("address").GetProperty("parts").EnumerateArray().Any(part => part.GetProperty("kind").GetString() == "Declaration" && part.GetProperty("key").GetString() == owner);

    protected WorkspaceSyntaxEntry Property(ScreenplayWorkspace workspace, string owner, string name, uint generation) => WorkspaceSyntaxIndex.Create(workspace).Entries.Single(entry =>
        entry.Node is PropertySyntax property && property.Name == name && entry.Parent is { } parent && WorkspaceSyntaxIndex.Create(workspace).Find(parent)?.Node is EventSyntax @event && @event.Name == owner && @event.Generation == generation);
}
