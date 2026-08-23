// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Defines how an identity assignment originated.
/// </summary>
public enum SemanticIdentityOrigin
{
    /// <summary>
    /// An unknown origin. Unknown values are never admitted.
    /// </summary>
    Unknown = -1,

    /// <summary>
    /// A durable assignment persisted by an authoring surface or migration.
    /// </summary>
    Persisted = 0,

    /// <summary>
    /// A deterministic provisional assignment for legacy source.
    /// </summary>
    LegacyBootstrap = 1
}

/// <summary>
/// Represents a document identity assignment.
/// </summary>
/// <param name="Key">The stable, non-path document key.</param>
/// <param name="Id">The assigned document identity.</param>
/// <param name="Origin">The assignment origin.</param>
public sealed record DocumentIdentityAssignment(string Key, DocumentId Id, SemanticIdentityOrigin Origin);

/// <summary>
/// Represents a semantic identity assignment.
/// </summary>
/// <param name="Address">The current semantic address.</param>
/// <param name="Id">The assigned semantic identity.</param>
/// <param name="Origin">The assignment origin.</param>
public sealed record SemanticIdentityAssignment(SemanticAddress Address, SemanticId Id, SemanticIdentityOrigin Origin);

/// <summary>
/// Represents an event contract identity assignment.
/// </summary>
/// <param name="Address">The current event declaration address.</param>
/// <param name="Id">The assigned event contract identity.</param>
/// <param name="Revision">The assigned event contract revision.</param>
/// <param name="Origin">The assignment origin.</param>
public sealed record EventContractIdentityAssignment(
    SemanticAddress Address,
    EventContractId Id,
    EventContractRevision Revision,
    SemanticIdentityOrigin Origin);

/// <summary>
/// Represents the immutable authoritative identity assignments for one semantic document set.
/// </summary>
public sealed class SemanticIdentityCatalog
{
    SemanticIdentityCatalog(
        ImmutableArray<DocumentIdentityAssignment> documents,
        ImmutableArray<SemanticIdentityAssignment> semantics,
        ImmutableArray<EventContractIdentityAssignment> eventContracts)
    {
        Documents = documents;
        Semantics = semantics;
        EventContracts = eventContracts;
    }

    /// <summary>
    /// Gets an empty identity catalog.
    /// </summary>
    public static SemanticIdentityCatalog Empty { get; } = Create([], [], []);

    /// <summary>
    /// Gets the document assignments.
    /// </summary>
    public ImmutableArray<DocumentIdentityAssignment> Documents { get; }

    /// <summary>
    /// Gets the semantic assignments.
    /// </summary>
    public ImmutableArray<SemanticIdentityAssignment> Semantics { get; }

    /// <summary>
    /// Gets the event contract assignments.
    /// </summary>
    public ImmutableArray<EventContractIdentityAssignment> EventContracts { get; }

    /// <summary>
    /// Creates and validates an immutable catalog.
    /// </summary>
    /// <param name="documents">The document assignments.</param>
    /// <param name="semantics">The semantic assignments.</param>
    /// <param name="eventContracts">The event contract assignments.</param>
    /// <returns>The validated catalog.</returns>
    /// <exception cref="InvalidSemanticContract">An array, assignment, identity, or origin is invalid or ambiguous.</exception>
    public static SemanticIdentityCatalog Create(
        ImmutableArray<DocumentIdentityAssignment> documents,
        ImmutableArray<SemanticIdentityAssignment> semantics,
        ImmutableArray<EventContractIdentityAssignment> eventContracts)
    {
        RejectDefault(documents, nameof(documents));
        RejectDefault(semantics, nameof(semantics));
        RejectDefault(eventContracts, nameof(eventContracts));

        var normalizedDocuments = documents.Select(Normalize).ToImmutableArray();
        ValidateDocumentAssignments(normalizedDocuments);
        ValidateSemanticAssignments(semantics);
        ValidateEventAssignments(eventContracts);

        return new(normalizedDocuments, [.. semantics], [.. eventContracts]);
    }

    /// <summary>
    /// Resolves a document identity, preferring an authoritative catalog assignment.
    /// </summary>
    /// <param name="stableKey">The stable, non-path document key.</param>
    /// <returns>The assigned identity, or a deterministic provisional identity.</returns>
    public DocumentId ResolveDocument(string stableKey)
    {
        var normalized = NormalizeKey(stableKey);
        var assignment = Documents.FirstOrDefault(_ => string.Equals(_.Key, normalized, StringComparison.Ordinal));
        return assignment?.Id ?? DocumentId.Create(normalized);
    }

    /// <summary>
    /// Resolves a semantic identity, preferring an authoritative catalog assignment.
    /// </summary>
    /// <param name="address">The current semantic address.</param>
    /// <returns>The assigned identity, or a deterministic provisional identity.</returns>
    public SemanticId ResolveSemantic(SemanticAddress address)
    {
        if (address is null)
        {
            throw new InvalidSemanticContract("A semantic identity resolution requires an address.");
        }

        var assignment = Semantics.FirstOrDefault(_ => _.Address.Equals(address));
        return assignment?.Id ?? SemanticId.Create(address);
    }

    /// <summary>
    /// Resolves an event contract assignment, preferring an authoritative catalog assignment.
    /// </summary>
    /// <param name="address">The current event declaration address.</param>
    /// <returns>The assigned contract identity and revision, or a deterministic legacy bootstrap assignment.</returns>
    public EventContractIdentityAssignment ResolveEventContract(SemanticAddress address)
    {
        if (address is null || address.Kind != SemanticKind.EventContract)
        {
            throw new InvalidSemanticContract("An event contract identity resolution requires an event contract address.");
        }

        var assignment = EventContracts.FirstOrDefault(_ => _.Address.Equals(address));
        return assignment ?? new(address, EventContractId.CreateLegacy(address), EventContractRevision.Initial, SemanticIdentityOrigin.LegacyBootstrap);
    }

    /// <summary>
    /// Verifies that every persisted assignment still identifies exactly one current address.
    /// </summary>
    /// <param name="documentKeys">The current document keys.</param>
    /// <param name="semanticAddresses">The current semantic addresses.</param>
    /// <param name="eventAddresses">The current event declaration addresses.</param>
    /// <exception cref="InvalidSemanticContract">Current inputs are ambiguous or an assignment is stale.</exception>
    public void VerifyAgainst(
        ImmutableArray<string> documentKeys,
        ImmutableArray<SemanticAddress> semanticAddresses,
        ImmutableArray<SemanticAddress> eventAddresses)
    {
        RejectDefault(documentKeys, nameof(documentKeys));
        RejectDefault(semanticAddresses, nameof(semanticAddresses));
        RejectDefault(eventAddresses, nameof(eventAddresses));

        var normalizedKeys = documentKeys.Select(NormalizeKey).ToImmutableArray();
        if (semanticAddresses.Any(_ => _ is null) || eventAddresses.Any(_ => _ is null || _.Kind != SemanticKind.EventContract))
        {
            throw new InvalidSemanticContract("Current identity addresses are malformed.");
        }

        RejectDuplicates(normalizedKeys, StringComparer.Ordinal, "current document key");
        RejectDuplicates(semanticAddresses, EqualityComparer<SemanticAddress>.Default, "current semantic address");
        RejectDuplicates(eventAddresses, EqualityComparer<SemanticAddress>.Default, "current event address");

        foreach (var assignment in Documents.Where(_ => !normalizedKeys.Contains(_.Key, StringComparer.Ordinal)))
        {
            throw new InvalidSemanticContract($"Document identity assignment '{assignment.Key}' is stale.");
        }

        foreach (var assignment in Semantics.Where(_ => !semanticAddresses.Contains(_.Address)))
        {
            throw new InvalidSemanticContract($"Semantic identity assignment '{assignment.Id}' is stale.");
        }

        foreach (var assignment in EventContracts.Where(_ => !eventAddresses.Contains(_.Address)))
        {
            throw new InvalidSemanticContract($"Event contract identity assignment '{assignment.Id}' is stale.");
        }
    }

    static DocumentIdentityAssignment Normalize(DocumentIdentityAssignment assignment)
    {
        if (assignment is null)
        {
            throw new InvalidSemanticContract("A document identity assignment cannot be null.");
        }

        return assignment with { Key = NormalizeKey(assignment.Key) };
    }

    static string NormalizeKey(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            throw new InvalidSemanticContract("A stable identity key cannot be empty.");
        }

        return key.Normalize(NormalizationForm.FormC);
    }

    static void ValidateDocumentAssignments(ImmutableArray<DocumentIdentityAssignment> assignments)
    {
        foreach (var assignment in assignments)
        {
            ValidateOrigin(assignment.Origin);
            if (!assignment.Id.IsSet)
            {
                throw new InvalidSemanticContract($"Document identity assignment '{assignment.Key}' has a default identity.");
            }
        }

        RejectDuplicates(assignments.Select(_ => _.Key), StringComparer.Ordinal, "document assignment key");
        RejectDuplicates(assignments.Select(_ => _.Id), EqualityComparer<DocumentId>.Default, "document identity");
    }

    static void ValidateSemanticAssignments(ImmutableArray<SemanticIdentityAssignment> assignments)
    {
        foreach (var assignment in assignments)
        {
            if (assignment is null || assignment.Address is null || !assignment.Id.IsSet)
            {
                throw new InvalidSemanticContract("A semantic identity assignment is malformed.");
            }

            ValidateOrigin(assignment.Origin);
        }

        RejectDuplicates(assignments.Select(_ => _.Address), EqualityComparer<SemanticAddress>.Default, "semantic assignment address");
        RejectDuplicates(assignments.Select(_ => _.Id), EqualityComparer<SemanticId>.Default, "semantic identity");
    }

    static void ValidateEventAssignments(ImmutableArray<EventContractIdentityAssignment> assignments)
    {
        foreach (var assignment in assignments)
        {
            if (assignment is null || assignment.Address is null || assignment.Address.Kind != SemanticKind.EventContract ||
                !assignment.Id.IsSet || assignment.Revision != EventContractRevision.Initial)
            {
                throw new InvalidSemanticContract("An event contract identity assignment is malformed.");
            }

            ValidateOrigin(assignment.Origin);
        }

        RejectDuplicates(assignments.Select(_ => _.Address), EqualityComparer<SemanticAddress>.Default, "event assignment address");
        RejectDuplicates(assignments.Select(_ => _.Id), EqualityComparer<EventContractId>.Default, "event contract identity");
    }

    static void ValidateOrigin(SemanticIdentityOrigin origin)
    {
        if (!Enum.IsDefined(origin) || origin == SemanticIdentityOrigin.Unknown)
        {
            throw new InvalidSemanticContract($"Semantic identity origin '{(int)origin}' is unknown.");
        }
    }

    static void RejectDefault<T>(ImmutableArray<T> values, string name)
    {
        if (values.IsDefault)
        {
            throw new InvalidSemanticContract($"The immutable array '{name}' cannot be default.");
        }
    }

    static void RejectDuplicates<T>(IEnumerable<T> values, IEqualityComparer<T> comparer, string description)
    {
        var seen = new HashSet<T>(comparer);
        foreach (var value in values)
        {
            if (!seen.Add(value))
            {
                throw new InvalidSemanticContract($"Duplicate {description} '{value}' is ambiguous.");
            }
        }
    }
}
