// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json.Serialization;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces;

/// <summary>
/// A durable, unverified modeling note, outside the executable model.
/// </summary>
/// <param name="Id">The random note identity.</param>
/// <param name="Kind">question, task or note.</param>
/// <param name="Text">Committed plain text.</param>
/// <param name="Anchor">The stable model anchor.</param>
/// <param name="LastAddress">The last known display address, never used for resolution.</param>
/// <param name="Author">Unverified client-supplied author.</param>
/// <param name="CreatedAt">The server UTC creation instant.</param>
/// <param name="Refs">External references, never fetched.</param>
/// <param name="Resolution">Null for an open note.</param>
public sealed record ModelNote(string Id, string Kind, string Text, ModelNotesAnchor Anchor, [property: JsonIgnore(Condition = JsonIgnoreCondition.Never)] ModelNotesAddress? LastAddress, string Author, DateTimeOffset CreatedAt, ImmutableArray<ModelNotesReference> Refs, [property: JsonIgnore(Condition = JsonIgnoreCondition.Never)] ModelNotesResolution? Resolution);

/// <summary>
/// Exactly one semantic identity or the whole application.
/// </summary>
/// <param name="SemanticId">The stable semantic identity.</param>
/// <param name="Application">True only for an application anchor.</param>
public sealed record ModelNotesAnchor(string? SemanticId = null, bool? Application = null);

/// <summary>
/// An external identifier and optional HTTP(S) URL.
/// </summary>
/// <param name="Id">The identifier.</param>
/// <param name="Url">The optional URL.</param>
public sealed record ModelNotesReference(string Id, string? Url = null);

/// <summary>
/// The explicit reason a note is resolved.
/// </summary>
/// <param name="By">specification, description, decision, external-decision or dismissed.</param>
/// <param name="Text">The explanation.</param>
/// <param name="Author">Unverified client-supplied author.</param>
/// <param name="At">The server UTC resolution instant.</param>
/// <param name="SemanticId">The model evidence identity, required for specification and description.</param>
/// <param name="Ref">The external decision reference.</param>
public sealed record ModelNotesResolution(string By, string Text, string Author, DateTimeOffset At, string? SemanticId = null, ModelNotesReference? Ref = null);

/// <summary>
/// A portable display address.
/// </summary>
/// <param name="Kind">The semantic kind.</param>
/// <param name="Parts">Typed address parts.</param>
public sealed record ModelNotesAddress(string Kind, ImmutableArray<ModelNotesAddressPart> Parts)
{
    /// <summary>
    /// Creates a display hint from a current semantic address.
    /// </summary>
    /// <param name="address">The current address.</param>
    /// <returns>The portable hint.</returns>
    public static ModelNotesAddress From(SemanticAddress address) => new(address.Kind.ToString(), [.. address.Parts.Select(part => new ModelNotesAddressPart(part.Kind.ToString(), part.Key))]);
}

/// <summary>
/// One typed display-address part.
/// </summary>
/// <param name="Kind">The part kind.</param>
/// <param name="Key">Its key.</param>
public sealed record ModelNotesAddressPart(string Kind, string Key);

/// <summary>
/// A validated notes sidecar.
/// </summary>
/// <param name="ApplicationIdentity">The identity scoping the sidecar.</param>
/// <param name="Notes">The durable notes, without computed states.</param>
public sealed record ModelNotesDocument(string ApplicationIdentity, ImmutableArray<ModelNote> Notes);

/// <summary>
/// The nonblocking result of reading a sidecar.
/// </summary>
/// <param name="Status">ok, absent or unreadable.</param>
/// <param name="Revision">Canonical notes revision; null when unreadable.</param>
/// <param name="Document">The validated document, if readable.</param>
/// <param name="Reason">The reason for an unreadable result.</param>
public sealed record ModelNotesRead(string Status, string? Revision, ModelNotesDocument? Document, string? Reason = null);

/// <summary>
/// A note with its current, computed anchor and state.
/// </summary>
/// <param name="Note">The durable note.</param>
/// <param name="Address">The resolved address, or null for an orphan.</param>
/// <param name="State">open, resolved or orphaned.</param>
/// <param name="OrphanReason">unknown for an identity absent from the catalog.</param>
public sealed record ModelNotesItem(ModelNote Note, ModelNotesAddress? Address, string State, string? OrphanReason)
{
    /// <summary>
    /// Gets whether the retained anchor has ambiguous source ownership. It remains resolved, not orphaned.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Ambiguous { get; init; }
}

/// <summary>
/// A host-only finding, never a compiler diagnostic.
/// </summary>
/// <param name="NoteId">The question identity.</param>
/// <param name="Severity">info.</param>
/// <param name="Code">model-note.open-question.</param>
/// <param name="Message">The question text.</param>
/// <param name="Address">The owning slice or container.</param>
public sealed record ModelNotesFinding(string NoteId, string Severity, string Code, string Message, ModelNotesAddress? Address);
