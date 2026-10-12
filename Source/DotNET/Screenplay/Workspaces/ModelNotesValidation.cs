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
    static readonly string[] _modelEvidence = ["specification", "description"];

    internal static void Document(ModelNotesDocument document)
    {
        _ = ApplicationIdentity.Parse(document.ApplicationIdentity);
        RejectAny(
            document,
            "Notes must contain at most 10000 unique note IDs.",
            [
                value => value.Notes.IsDefault,
                value => value.Notes.Length > 10000,
                HasDuplicateIds
            ]);
        foreach (var note in document.Notes) Note(note);
    }

    internal static void Note(ModelNote note)
    {
        NoteId(note.Id);
        Choice(note.Kind, ["question", "task", "note"]);
        Text(note.Text);
        Text(note.Author);
        Anchor(note.Anchor);
        Instant(note.CreatedAt);
        References(note);
        ValidateOptional(note.LastAddress, Address);
        ValidateOptional(note.Resolution, Resolution);
    }

    internal static void Resolution(ModelNotesResolution resolution)
    {
        Choice(resolution.By, ["specification", "description", "decision", "external-decision", "dismissed"]);
        Text(resolution.Text);
        Text(resolution.Author);
        Instant(resolution.At);
        ValidateOptional(resolution.SemanticId, value => _ = SemanticId.Parse(value));
        RequireModelEvidence(resolution);
        RequireExternalDecision(resolution);
        ValidateOptional(resolution.Ref, Reference);
    }

    internal static void Anchor(ModelNotesAnchor anchor)
    {
        RejectAny(
            anchor,
            "An anchor requires exactly semanticId or application: true.",
            [
                value => value is null,
                value => (value.SemanticId is null) == (value.Application != true),
                value => value.Application == false
            ]);
        ValidateOptional(anchor.SemanticId, value => _ = SemanticId.Parse(value));
    }

    internal static void Text(string value) => RejectAny(
        value,
        "Text must be nonempty, trimmed and at most 16384 characters.",
        [
            string.IsNullOrWhiteSpace,
            text => text != text.Trim(),
            text => text.Length > 16384
        ]);

    internal static void UniqueProperties(JsonElement value)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Object: ObjectProperties(value); break;
            case JsonValueKind.Array:
                foreach (var item in value.EnumerateArray()) UniqueProperties(item);
                break;
        }
    }

    static void RejectAny<T>(T value, string message, IEnumerable<Func<T, bool>> rules)
    {
        if (rules.Any(rule => rule(value))) throw new InvalidModelNotes(message);
    }

    static void ValidateOptional<T>(T? value, Action<T> validate)
        where T : class
    {
        if (value is not null) validate(value);
    }

    static bool HasDuplicateIds(ModelNotesDocument document) => document.Notes.Select(note => note.Id).Distinct(StringComparer.Ordinal).Count() != document.Notes.Length;

    static void NoteId(string id) => RejectAny(
        id,
        "A note ID must be note_ followed by a 22-character URL-safe random token.",
        [
            value => value is null,
            value => value.Length != 27,
            value => !value.StartsWith("note_", StringComparison.Ordinal),
            value => value[5..].Any(character => !IsTokenCharacter(character))
        ]);

    static bool IsTokenCharacter(char character) => char.IsAsciiLetterOrDigit(character) || "-_".Contains(character);

    static void References(ModelNote note)
    {
        RejectAny(note, "A note requires at most 100 references.", [value => value.Refs.IsDefault, value => value.Refs.Length > 100]);
        foreach (var reference in note.Refs) Reference(reference);
    }

    static void RequireModelEvidence(ModelNotesResolution resolution)
    {
        if (!_modelEvidence.Contains(resolution.By, StringComparer.Ordinal)) return;
        if (resolution.SemanticId is null) throw new InvalidModelNotes("Model evidence requires semanticId.");
    }

    static void RequireExternalDecision(ModelNotesResolution resolution)
    {
        if (resolution.By != "external-decision") return;
        if (resolution.Ref is null) throw new InvalidModelNotes("An external decision requires ref.");
    }

    static void ObjectProperties(JsonElement value)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in value.EnumerateObject())
        {
            RequireUniqueName(names, property.Name);
            UniqueProperties(property.Value);
        }
    }

    static void RequireUniqueName(HashSet<string> names, string name)
    {
        if (!names.Add(name)) throw new InvalidModelNotes($"Duplicate JSON property '{name}'.");
    }

    static void Reference(ModelNotesReference reference)
    {
        Text(reference.Id);
        ValidateOptional(reference.Url, Url);
    }

    static void Url(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)) throw new InvalidModelNotes("Reference URLs must be absolute HTTP(S) URLs.");
        if (!new[] { "http", "https" }.Contains(uri.Scheme, StringComparer.Ordinal)) throw new InvalidModelNotes("Reference URLs must be absolute HTTP(S) URLs.");
    }

    static void Address(ModelNotesAddress address)
    {
        RejectAny(address, "Invalid lastAddress.", [value => !KnownKind<SemanticKind>(value.Kind), value => value.Parts.IsDefaultOrEmpty]);
        foreach (var part in address.Parts) AddressPart(part);
    }

    static void AddressPart(ModelNotesAddressPart part)
    {
        if (!KnownKind<SemanticAddressPartKind>(part.Kind)) throw new InvalidModelNotes("Invalid lastAddress part.");
        Text(part.Key);
    }

    static bool KnownKind<T>(string text)
        where T : struct, Enum
    {
        if (!Enum.TryParse<T>(text, out var kind)) return false;
        if (EqualityComparer<T>.Default.Equals(kind, Enum.Parse<T>("Unknown"))) return false;
        return Enum.IsDefined(kind);
    }

    static void Instant(DateTimeOffset instant) => RejectAny(instant, "Note timestamps must be UTC instants.", [value => value == default, value => value.Offset != TimeSpan.Zero]);

    static void Choice(string value, string[] choices)
    {
        if (!choices.Contains(value, StringComparer.Ordinal)) throw new InvalidModelNotes($"Unknown notes value '{value}'.");
    }
}
