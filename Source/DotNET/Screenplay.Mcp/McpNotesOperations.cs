// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

sealed class McpNotesOperations(ScreenplayWorkspace workspace, List<object> substitutions)
{
    internal static ModelNotesDocument Apply(ModelNotesDocument before, ScreenplayWorkspace workspace, JsonElement operations, List<object> substitutions)
    {
        if (operations.ValueKind != JsonValueKind.Array || operations.GetArrayLength() is < 1 or > 200) throw new McpFailure("Supply 1–200 note operations.", -32602);
        var editor = new McpNotesOperations(workspace, substitutions);
        var notes = before.Notes.ToDictionary(note => note.Id, StringComparer.Ordinal);
        foreach (var operation in operations.EnumerateArray())
        {
            McpJson.ValidateObject(operation, ["operation", "id", "kind", "text", "author", "refs", "anchor", "target", "resolution"], ["operation"]);
            var action = McpJson.RequiredString(operation, "operation");
            if (action == "add")
            {
                var note = editor.Add(operation);
                notes.Add(note.Id, note);
                continue;
            }

            var id = McpJson.RequiredString(operation, "id");
            if (!notes.TryGetValue(id, out var existing)) throw new McpFailure($"Unknown note '{id}'.", -32602);
            if (action == "remove") notes.Remove(id);
            else notes[id] = Refresh(editor.Change(existing, action, operation), workspace);
        }

        var after = before with { Notes = [.. notes.Values.Select(note => Refresh(note, workspace))] };
        _ = ModelNotesStore.Serialize(after);
        return after;
    }

    static ModelNotesResolution Resolution(JsonElement operation, ScreenplayWorkspace workspace)
    {
        var input = operation.GetProperty("resolution");
        McpJson.ValidateObject(input, ["by", "text", "author", "semanticId", "ref"], ["by", "text", "author"]);
        var resolution = new ModelNotesResolution(
            McpJson.RequiredString(input, "by"),
            McpJson.RequiredString(input, "text"),
            McpJson.RequiredString(input, "author"),
            DateTimeOffset.UtcNow,
            McpJson.OptionalString(input, "semanticId"),
            input.TryGetProperty("ref", out var reference) ? reference.Deserialize<ModelNotesReference>(ModelNotesStore.Options) : null);
        ModelNotesValidation.Resolution(resolution);
        if (resolution.SemanticId is not null && !workspace.IdentityCatalog.Semantics.Any(assignment => assignment.Id.ToString() == resolution.SemanticId)) throw new McpFailure("Unknown resolution semanticId.", -32602);
        return resolution;
    }

    static ImmutableArray<ModelNotesReference> Refs(JsonElement operation) => operation.TryGetProperty("refs", out var refs)
        ? refs.Deserialize<ImmutableArray<ModelNotesReference>>(ModelNotesStore.Options) : [];

    static ModelNote Refresh(ModelNote note, ScreenplayWorkspace workspace)
    {
        var address = ModelNotesAnchors.Resolve(note.Anchor, workspace.IdentityCatalog);
        return note with { LastAddress = address is null ? note.LastAddress : ModelNotesAddress.From(address) };
    }

    ModelNote Add(JsonElement operation)
    {
        var note = new ModelNote(
            ModelNotesStore.NewId(),
            McpJson.RequiredString(operation, "kind"),
            McpJson.RequiredString(operation, "text"),
            Anchor(operation),
            null,
            McpJson.RequiredString(operation, "author"),
            DateTimeOffset.UtcNow,
            Refs(operation),
            null);
        return Refresh(note, workspace);
    }

    ModelNote Change(ModelNote note, string action, JsonElement operation) => action switch
    {
        "edit" => note with
        {
            Kind = McpJson.OptionalString(operation, "kind") ?? note.Kind,
            Text = McpJson.OptionalString(operation, "text") ?? note.Text,
            Refs = operation.TryGetProperty("refs", out _) ? Refs(operation) : note.Refs
        },
        "resolve" => note with { Resolution = Resolution(operation, workspace) },
        "reopen" => note with { Resolution = null },
        "reanchor" => note with { Anchor = Anchor(operation) },
        _ => throw new McpFailure($"Unknown note operation '{action}'.", -32602)
    };

    ModelNotesAnchor Anchor(JsonElement operation)
    {
        if (operation.TryGetProperty("anchor", out var anchor) == operation.TryGetProperty("target", out var target)) throw new McpFailure("Supply exactly one anchor or target.", -32602);
        var value = anchor.ValueKind == JsonValueKind.Undefined ? target : anchor;
        var resolved = McpNotesTargets.Read(value, workspace);
        if (McpNotesTargets.Substituted(value, resolved, workspace)) substitutions.Add(new { target = value, anchor = resolved, address = ModelNotesAddress.From(ModelNotesAnchors.Resolve(resolved, workspace.IdentityCatalog)!), reason = "Syntax-only target anchored to its nearest identified container." });
        return resolved;
    }
}
