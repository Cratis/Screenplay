// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces;

/// <summary>
/// The exception that is thrown when a notes document or proposed note is invalid.
/// </summary>
/// <param name="message">The validation failure.</param>
public sealed class InvalidModelNotes(string message) : Exception(message);

internal static class ModelNotesValidation
{
    internal static void Document(ModelNotesDocument document)
    {
        _ = ApplicationIdentity.Parse(document.ApplicationIdentity);
        if (document.Notes.IsDefault || document.Notes.Length > 10000 || document.Notes.Select(note => note.Id).Distinct(StringComparer.Ordinal).Count() != document.Notes.Length)
        {
            throw new InvalidModelNotes("Notes must contain at most 10000 unique note IDs.");
        }

        foreach (var note in document.Notes) Note(note);
    }

    internal static void Note(ModelNote note)
    {
        if (note.Id is null || note.Id.Length != 27 || !note.Id.StartsWith("note_", StringComparison.Ordinal) || note.Id[5..].Any(character => !char.IsAsciiLetterOrDigit(character) && character is not '-' and not '_'))
        {
            throw new InvalidModelNotes("A note ID must be note_ followed by a 22-character URL-safe random token.");
        }

        Choice(note.Kind, ["question", "task", "note"]);
        Text(note.Text);
        Text(note.Author);
        Anchor(note.Anchor);
        Instant(note.CreatedAt);
        if (note.Refs.IsDefault || note.Refs.Length > 100) throw new InvalidModelNotes("A note requires at most 100 references.");
        foreach (var reference in note.Refs) Reference(reference);
        if (note.LastAddress is not null) Address(note.LastAddress);
        if (note.Resolution is not null) Resolution(note.Resolution);
    }

    internal static void Resolution(ModelNotesResolution resolution)
    {
        Choice(resolution.By, ["specification", "description", "decision", "external-decision", "dismissed"]);
        Text(resolution.Text);
        Text(resolution.Author);
        Instant(resolution.At);
        if (resolution.SemanticId is not null) _ = SemanticId.Parse(resolution.SemanticId);
        if ((resolution.By == "specification" || resolution.By == "description") && resolution.SemanticId is null) throw new InvalidModelNotes("Model evidence requires semanticId.");
        if (resolution.By == "external-decision" && resolution.Ref is null) throw new InvalidModelNotes("An external decision requires ref.");
        if (resolution.Ref is not null) Reference(resolution.Ref);
    }

    internal static void Anchor(ModelNotesAnchor anchor)
    {
        if (anchor is null || (anchor.SemanticId is null) == (anchor.Application != true) || anchor.Application == false)
        {
            throw new InvalidModelNotes("An anchor requires exactly semanticId or application: true.");
        }

        if (anchor.SemanticId is not null) _ = SemanticId.Parse(anchor.SemanticId);
    }

    internal static void Text(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value != value.Trim() || value.Length > 16384)
        {
            throw new InvalidModelNotes("Text must be nonempty, trimmed and at most 16384 characters.");
        }
    }

    internal static void UniqueProperties(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in value.EnumerateObject())
            {
                if (!names.Add(property.Name)) throw new InvalidModelNotes($"Duplicate JSON property '{property.Name}'.");
                UniqueProperties(property.Value);
            }
        }
        else if (value.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in value.EnumerateArray()) UniqueProperties(item);
        }
    }

    static void Reference(ModelNotesReference reference)
    {
        Text(reference.Id);
        if (reference.Url is not null && (!Uri.TryCreate(reference.Url, UriKind.Absolute, out var uri) || uri.Scheme is not "http" and not "https"))
        {
            throw new InvalidModelNotes("Reference URLs must be absolute HTTP(S) URLs.");
        }
    }

    static void Address(ModelNotesAddress address)
    {
        if (!Enum.TryParse<SemanticKind>(address.Kind, out var kind) || kind == SemanticKind.Unknown || !Enum.IsDefined(kind) || address.Parts.IsDefaultOrEmpty)
        {
            throw new InvalidModelNotes("Invalid lastAddress.");
        }

        foreach (var part in address.Parts)
        {
            if (!Enum.TryParse<SemanticAddressPartKind>(part.Kind, out var partKind) || !Enum.IsDefined(partKind) || partKind == SemanticAddressPartKind.Unknown)
            {
                throw new InvalidModelNotes("Invalid lastAddress part.");
            }

            Text(part.Key);
        }
    }

    static void Instant(DateTimeOffset instant)
    {
        if (instant == default || instant.Offset != TimeSpan.Zero) throw new InvalidModelNotes("Note timestamps must be UTC instants.");
    }

    static void Choice(string value, string[] choices)
    {
        if (!choices.Contains(value, StringComparer.Ordinal)) throw new InvalidModelNotes($"Unknown notes value '{value}'.");
    }
}
