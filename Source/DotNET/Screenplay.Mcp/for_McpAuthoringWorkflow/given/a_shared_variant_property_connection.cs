// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow.given;

public class a_shared_variant_property_connection : an_authoring_connection
{
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
        File.WriteAllText(Path.Combine(RootPath, "application.play"), Workspaces.for_WorkspaceRefactoring.given.a_shared_variant_automap_workspace.VariantAutoMapSource, new UTF8Encoding(false));
        Initialize();
    }

    protected void Rename(bool advance)
    {
        var opened = Open();
        Before = Workspace();
        var target = Result("read-ast", new { expectedRevision = opened.GetProperty("revision").GetString(), kind = "PropertySyntax", name = "name" }).GetProperty("page").GetProperty("items").EnumerateArray()
            .Single(node => node.GetProperty("handle").GetProperty("path").GetString().Contains("/events/", StringComparison.Ordinal)).GetProperty("handle");
        Proposal = Result("propose-property", new
        {
            expectedRevision = opened.GetProperty("revision").GetString(),
            expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
            action = "rename", target, expectedName = "name", newName = "label",
            newGeneration = advance, eventNeverPersisted = !advance, formatting = "CanonicalizeTouchedDocuments"
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
        Reopened = McpState.Deserialize(File.ReadAllBytes(Path.Combine(RootPath, ".screenplay", "identities.json"))).Open(Root);
    }

    protected object[] Properties(ScreenplayWorkspace workspace) => WorkspaceSyntaxIndex.Create(workspace).Entries
        .Where(entry => entry.Node is PropertySyntax).OrderBy(entry => entry.Address!, WorkspaceSemanticAddressComparer.Instance)
        .Select(entry => (object)(entry.Address, entry.SemanticId)).ToArray();

    protected object[] VariantProperties(ScreenplayWorkspace workspace) => WorkspaceSyntaxIndex.Create(workspace).Entries
        .Where(entry => entry.Node is PropertySyntax && entry.Address?.OwnerKind == Semantics.SemanticKind.ReadModel).OrderBy(entry => entry.Address!, WorkspaceSemanticAddressComparer.Instance)
        .Select(entry => (object)(entry.Address, entry.SemanticId)).ToArray();

    protected int PhysicalMappings => WorkspaceSyntaxIndex.Create(Reopened).Entries.Count(entry => entry.Node is SetMappingSyntax);
}
