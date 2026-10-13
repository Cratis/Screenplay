// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces;

/// <summary>A resolved stored event shape, independent of authoring identities and type names.</summary>
/// <param name="EventType">The effective stored name.</param>
/// <param name="Properties">The sorted stored properties.</param>
public sealed record ReleaseSchema(string EventType, ImmutableArray<ReleaseSchemaProperty> Properties);

/// <summary>A stored property.</summary>
/// <param name="Name">The stored name.</param>
/// <param name="Optional">Whether null is allowed.</param>
/// <param name="Collection">Whether the value is a collection.</param>
/// <param name="Type">The expanded value shape.</param>
public sealed record ReleaseSchemaProperty(string Name, bool Optional, bool Collection, ReleaseSchemaType Type);

/// <summary>An expanded primitive, enumeration or composite value.</summary>
/// <param name="Kind">primitive, enum or composite.</param>
/// <param name="Primitive">The primitive name, when primitive.</param>
/// <param name="Compliance">The concept compliance marker, when present.</param>
/// <param name="Members">The enum members, when enum.</param>
/// <param name="Properties">The composite members, when composite.</param>
public sealed record ReleaseSchemaType(string Kind, string? Primitive = null, SemanticConceptCompliance? Compliance = null, ImmutableArray<ReleaseSchemaMember>? Members = null, ImmutableArray<ReleaseSchemaProperty>? Properties = null);

/// <summary>A stored enum member.</summary>
/// <param name="Name">The member name.</param>
/// <param name="Number">Its stored int32 identity.</param>
public sealed record ReleaseSchemaMember(string Name, int Number);

/// <summary>One immutable admitted event revision.</summary>
/// <param name="Event">The event contract identity.</param>
/// <param name="Revision">The contract revision.</param>
/// <param name="EventType">The stored name.</param>
/// <param name="SchemaHash">The resolved schema hash.</param>
/// <param name="Schema">The resolved shape.</param>
/// <param name="LastAddress">A display hint, never a matching key.</param>
/// <param name="AdmittedIn">The original release label.</param>
/// <param name="AdmittedAt">The UTC admission instant.</param>
public sealed record ReleaseBaselineEntry(string Event, uint Revision, string EventType, string SchemaHash, ReleaseSchema Schema, ModelNotesAddress LastAddress, string AdmittedIn, DateTimeOffset AdmittedAt);

/// <summary>The committed release baseline.</summary>
/// <param name="ApplicationIdentity">The owning application identity.</param>
/// <param name="Entries">The admitted revisions.</param>
public sealed record ReleaseBaselineDocument(string ApplicationIdentity, ImmutableArray<ReleaseBaselineEntry> Entries);

/// <summary>An explicit nonblocking sidecar read outcome.</summary>
/// <param name="Status">absent, valid or unreadable.</param>
/// <param name="Revision">The canonical revision, null when unreadable.</param>
/// <param name="Document">The readable document.</param>
/// <param name="Reason">The unreadable reason.</param>
public sealed record ReleaseBaselineRead(string Status, string? Revision, ReleaseBaselineDocument? Document, string? Reason = null);

/// <summary>A structural violation of an admitted revision.</summary>
/// <param name="Kind">BaselinedRevisionChanged or BaselinedRevisionMissing.</param>
/// <param name="Event">The contract identity.</param>
/// <param name="Revision">The frozen revision.</param>
/// <param name="Path">The first differing stored path, when changed.</param>
public sealed record ReleaseBaselineViolation(string Kind, string Event, uint Revision, string? Path = null);

/// <summary>A typed check or admission outcome; no disk writes are performed.</summary>
/// <param name="Document">The admission candidate, if successful.</param>
/// <param name="Violations">All affected frozen revisions.</param>
/// <param name="Failure">An expected refusal kind, or null on success.</param>
/// <param name="Reason">The refusal details.</param>
public sealed record ReleaseBaselineResult(ReleaseBaselineDocument? Document, ImmutableArray<ReleaseBaselineViolation> Violations, string? Failure = null, string? Reason = null)
{
    /// <summary>Gets whether the operation succeeded.</summary>
    public bool Success => Failure is null && Violations.IsEmpty;
}
