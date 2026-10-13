// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

sealed record ModelNotesInput(McpRoot Root, McpState? State)
{
    internal string Name => State?.ApplicationName ?? new DirectoryInfo(Root.DirectoryPath).Name;
    internal ApplicationIdentity Identity => State?.Catalog.Application ?? ApplicationIdentity.Create(Name);
    internal ModelNotesRead Notes => State is null ? UnscopedNotes() : ModelNotesStore.Read(Root.DirectoryPath, Identity);
    internal string? OrphanReason => State is null ? "identity-state-absent" : null;

    internal static ModelNotesInput Read(string directory)
    {
        var root = new McpRoot(directory);
        var persisted = new McpManagedFiles(root).Read(McpState.FileName);

        return new(root, persisted is null ? null : McpState.Deserialize(persisted));
    }

    internal ImmutableArray<ModelNotesFinding> Findings(ModelNotesDocument notes, string? scope)
    {
        if (State is not null) return ModelNotesAnchors.Findings(notes, State.Open(Root).IdentityCatalog, scope);

        return [.. notes.Notes.Where(note => note.Kind == "question" && note.Resolution is null).OrderBy(note => note.Id, StringComparer.Ordinal)
            .Select(note => new ModelNotesFinding(note.Id, "info", "model-note.open-question", note.Text, null))];
    }

    ModelNotesRead UnscopedNotes()
    {
        var bytes = new McpManagedFiles(Root).Read(ModelNotesStore.FileName, ModelNotesStore.MaximumBytes);
        if (bytes is null) return ModelNotesStore.ReadBytes(null, Identity);
        try
        {
            using var json = JsonDocument.Parse(bytes);
            var application = ApplicationIdentity.Parse(json.RootElement.GetProperty("applicationIdentity").GetString()!);

            return ModelNotesStore.ReadBytes(bytes, application);
        }
        catch (Exception exception) when (exception is JsonException or KeyNotFoundException or InvalidOperationException or InvalidSemanticContract)
        {
            return new("unreadable", null, null, exception.Message);
        }
    }
}
