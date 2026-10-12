// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_ModelNotes.given;

public class a_notes_workspace : for_McpDurableState.given.a_durable_workspace
{
    internal McpWorkspaces Workspaces = null!;
    internal JsonElement Opened;
    internal ModelNotesDocument Notes = null!;
    internal byte[] NotesBytes = [];

    void Establish()
    {
        var slice = Original.IdentityCatalog.Semantics.Single(assignment => assignment.Address.Kind == Semantics.SemanticKind.Slice);
        Notes = new(Original.IdentityCatalog.Application.ToString(), [new(ModelNotesStore.NewId(), "question", "May a member cancel?", new(slice.Id.ToString()), ModelNotesAddress.From(slice.Address), "sindre", DateTimeOffset.UtcNow, [new("D123")], null)]);
        NotesBytes = ModelNotesStore.Serialize(Notes);
        McpManagedFiles.WritePrivate(Files.PathFor(ModelNotesStore.FileName), NotesBytes);
        Workspaces = new(Root);
        Opened = Result(Workspaces.Open(McpJson.Empty));
    }

    internal static new JsonElement Arguments(object value) => JsonSerializer.SerializeToElement(value, McpJson.Options);

    internal JsonElement ProposeNotes(object[] operations) => Result(Workspaces.ProposeNotes(Arguments(new
    {
        expectedRevision = Opened.GetProperty("revision").GetString(),
        expectedCatalogRevision = Opened.GetProperty("catalogRevision").GetString(),
        expectedNotesRevision = Opened.GetProperty("notesRevision").GetString(),
        operations
    })));

    internal JsonElement Apply(JsonElement proposal) => Result(Workspaces.Apply(Arguments(new
    {
        proposalId = proposal.GetProperty("proposalId").GetString(),
        expectedRevision = Opened.GetProperty("revision").GetString(),
        expectedCatalogRevision = Opened.GetProperty("catalogRevision").GetString()
    })));
}
