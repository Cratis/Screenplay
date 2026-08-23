// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Cratis.Screenplay.Semantics.Serialization;

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
/// Represents an explicit document-key rename.
/// </summary>
/// <param name="PreviousKey">The key in the base catalog.</param>
/// <param name="CurrentKey">The key in the planned catalog.</param>
public sealed record DocumentIdentityRename(string PreviousKey, string CurrentKey);

/// <summary>
/// Represents an explicit semantic-address rename.
/// </summary>
/// <param name="PreviousAddress">The address in the base catalog.</param>
/// <param name="CurrentAddress">The address in the planned catalog.</param>
public sealed record SemanticIdentityRename(SemanticAddress PreviousAddress, SemanticAddress CurrentAddress);

/// <summary>
/// Represents an explicit event-contract address rename.
/// </summary>
/// <param name="PreviousAddress">The address in the base catalog.</param>
/// <param name="CurrentAddress">The address in the planned catalog.</param>
public sealed record EventContractIdentityRename(SemanticAddress PreviousAddress, SemanticAddress CurrentAddress);

/// <summary>
/// Represents a deterministic catalog migration plan tied to a base revision.
/// </summary>
/// <param name="BaseRevision">The exact catalog revision used to create the plan.</param>
/// <param name="Catalog">The planned catalog.</param>
public sealed record SemanticIdentityCatalogMigrationPlan(CatalogRevision BaseRevision, SemanticIdentityCatalog Catalog);

/// <summary>
/// Represents the immutable authoritative identity assignments for one application.
/// </summary>
public sealed class SemanticIdentityCatalog
{
    SemanticIdentityCatalog(
        ApplicationIdentity application,
        ImmutableArray<DocumentIdentityAssignment> documents,
        ImmutableArray<SemanticIdentityAssignment> semantics,
        ImmutableArray<EventContractIdentityAssignment> eventContracts,
        CatalogRevision revision)
    {
        Application = application;
        Documents = documents;
        Semantics = semantics;
        EventContracts = eventContracts;
        Revision = revision;
    }

    /// <summary>
    /// Gets the application identity that scopes all assignments.
    /// </summary>
    public ApplicationIdentity Application { get; }

    /// <summary>
    /// Gets the deterministic catalog revision.
    /// </summary>
    public CatalogRevision Revision { get; }

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
    /// Creates an empty identity catalog for an application.
    /// </summary>
    /// <param name="application">The application identity.</param>
    /// <returns>The empty catalog.</returns>
    public static SemanticIdentityCatalog Empty(ApplicationIdentity application) => Create(application, [], [], []);

    /// <summary>
    /// Creates and validates an immutable catalog.
    /// </summary>
    /// <param name="application">The application identity.</param>
    /// <param name="documents">The document assignments.</param>
    /// <param name="semantics">The semantic assignments.</param>
    /// <param name="eventContracts">The event contract assignments.</param>
    /// <returns>The validated catalog.</returns>
    /// <exception cref="InvalidSemanticContract">An array, assignment, identity, origin, or bootstrap derivation is invalid or ambiguous.</exception>
    public static SemanticIdentityCatalog Create(
        ApplicationIdentity application,
        ImmutableArray<DocumentIdentityAssignment> documents,
        ImmutableArray<SemanticIdentityAssignment> semantics,
        ImmutableArray<EventContractIdentityAssignment> eventContracts)
    {
        if (!application.IsSet)
        {
            throw new InvalidSemanticContract("A semantic identity catalog requires an application identity.");
        }

        RejectDefault(documents, nameof(documents));
        RejectDefault(semantics, nameof(semantics));
        RejectDefault(eventContracts, nameof(eventContracts));

        var normalizedDocuments = documents.Select(Normalize).ToImmutableArray();
        ValidateDocumentAssignments(normalizedDocuments);
        ValidateSemanticAssignments(application, semantics);
        ValidateEventAssignments(application, eventContracts);

        var catalog = new SemanticIdentityCatalog(application, normalizedDocuments, [.. semantics], [.. eventContracts], default);
        return new(application, normalizedDocuments, [.. semantics], [.. eventContracts], SemanticIdentityCatalogSerializer.ComputeRevision(catalog));
    }

    /// <summary>
    /// Plans an explicit, deterministic, one-to-one migration from a base catalog revision.
    /// </summary>
    /// <param name="previous">The previous authoritative catalog.</param>
    /// <param name="baseRevision">The exact revision of <paramref name="previous"/>.</param>
    /// <param name="documentKeys">The complete current document keys.</param>
    /// <param name="semanticAddresses">The complete current semantic addresses.</param>
    /// <param name="eventAddresses">The complete current event addresses.</param>
    /// <param name="documentRenames">Explicit document renames.</param>
    /// <param name="semanticRenames">Explicit semantic renames.</param>
    /// <param name="eventRenames">Explicit event-contract renames.</param>
    /// <returns>The revision-bound migration plan.</returns>
    /// <exception cref="InvalidSemanticContract">The base is stale or a rename is stale, ambiguous, duplicated, guessed, or incomplete.</exception>
    public static SemanticIdentityCatalogMigrationPlan PlanMigration(
        SemanticIdentityCatalog previous,
        CatalogRevision baseRevision,
        ImmutableArray<string> documentKeys,
        ImmutableArray<SemanticAddress> semanticAddresses,
        ImmutableArray<SemanticAddress> eventAddresses,
        ImmutableArray<DocumentIdentityRename> documentRenames,
        ImmutableArray<SemanticIdentityRename> semanticRenames,
        ImmutableArray<EventContractIdentityRename> eventRenames)
    {
        if (previous is null || !baseRevision.IsSet || previous.Revision != baseRevision)
        {
            throw new InvalidSemanticContract("The identity catalog migration base revision is stale or does not identify the supplied catalog.");
        }

        RejectDefault(documentKeys, nameof(documentKeys));
        RejectDefault(semanticAddresses, nameof(semanticAddresses));
        RejectDefault(eventAddresses, nameof(eventAddresses));
        RejectDefault(documentRenames, nameof(documentRenames));
        RejectDefault(semanticRenames, nameof(semanticRenames));
        RejectDefault(eventRenames, nameof(eventRenames));

        var normalizedKeys = documentKeys.Select(NormalizeKey).ToImmutableArray();
        ValidateCurrent(previous.Application, normalizedKeys, semanticAddresses, eventAddresses);
        var normalizedDocumentRenames = documentRenames.Select(Normalize).ToImmutableArray();
        ValidateRenames(previous, normalizedKeys, semanticAddresses, eventAddresses, normalizedDocumentRenames, semanticRenames, eventRenames);

        var documents = PlanDocuments(previous, normalizedKeys, normalizedDocumentRenames);
        var semantics = PlanSemantics(previous, semanticAddresses, semanticRenames);
        var events = PlanEvents(previous, eventAddresses, eventRenames);
        var catalog = Create(previous.Application, documents, semantics, events);
        return new(baseRevision, catalog);
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
        RequireApplicationAddress(Application, address);
        var assignment = Semantics.FirstOrDefault(_ => _.Address.Equals(address));
        return assignment?.Id ?? SemanticId.Create(address);
    }

    /// <summary>
    /// Resolves an event contract assignment, preferring an authoritative catalog assignment.
    /// </summary>
    /// <param name="address">The current event declaration address.</param>
    /// <returns>The assigned contract identity and revision, or the deterministic legacy bootstrap assignment.</returns>
    public EventContractIdentityAssignment ResolveEventContract(SemanticAddress address)
    {
        RequireApplicationAddress(Application, address);
        if (address.Kind != SemanticKind.EventContract)
        {
            throw new InvalidSemanticContract("An event contract identity resolution requires an event contract address.");
        }

        var assignment = EventContracts.FirstOrDefault(_ => _.Address.Equals(address));
        return assignment ?? new(
            address,
            EventContractId.CreateLegacy(Application, address.Name),
            EventContractRevision.Initial,
            SemanticIdentityOrigin.LegacyBootstrap);
    }

    /// <summary>
    /// Verifies that every assignment still identifies exactly one current address.
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
        ValidateCurrent(Application, normalizedKeys, semanticAddresses, eventAddresses);
        if (Documents.Any(_ => !normalizedKeys.Contains(_.Key, StringComparer.Ordinal)) ||
            Semantics.Any(_ => !semanticAddresses.Contains(_.Address)) ||
            EventContracts.Any(_ => !eventAddresses.Contains(_.Address)))
        {
            throw new InvalidSemanticContract("The identity catalog contains a stale assignment; continuity requires an explicit migration plan.");
        }

        ValidateEffectiveEventContracts(eventAddresses.Select(ResolveEventContract));
    }

    static ImmutableArray<DocumentIdentityAssignment> PlanDocuments(
        SemanticIdentityCatalog previous,
        ImmutableArray<string> current,
        ImmutableArray<DocumentIdentityRename> renames)
    {
        var assignments = current.Select(key =>
        {
            var unchanged = previous.Documents.FirstOrDefault(_ => _.Key == key);
            if (unchanged is not null)
            {
                return unchanged;
            }

            var rename = renames.FirstOrDefault(_ => _.CurrentKey == key);
            if (rename is not null)
            {
                var assignment = previous.Documents.Single(_ => _.Key == rename.PreviousKey);
                return assignment with { Key = key, Origin = SemanticIdentityOrigin.Persisted };
            }

            return new DocumentIdentityAssignment(key, DocumentId.Create(key), SemanticIdentityOrigin.LegacyBootstrap);
        }).OrderBy(_ => _.Id.ToString(), StringComparer.Ordinal);
        return [.. assignments];
    }

    static ImmutableArray<SemanticIdentityAssignment> PlanSemantics(
        SemanticIdentityCatalog previous,
        ImmutableArray<SemanticAddress> current,
        ImmutableArray<SemanticIdentityRename> renames)
    {
        var assignments = current.Select(address =>
        {
            var unchanged = previous.Semantics.FirstOrDefault(_ => _.Address.Equals(address));
            if (unchanged is not null)
            {
                return unchanged;
            }

            var rename = renames.FirstOrDefault(_ => _.CurrentAddress.Equals(address));
            if (rename is not null)
            {
                var assignment = previous.Semantics.Single(_ => _.Address.Equals(rename.PreviousAddress));
                return assignment with { Address = address, Origin = SemanticIdentityOrigin.Persisted };
            }

            return new SemanticIdentityAssignment(address, SemanticId.Create(address), SemanticIdentityOrigin.LegacyBootstrap);
        }).OrderBy(_ => _.Id.ToString(), StringComparer.Ordinal);
        return [.. assignments];
    }

    static ImmutableArray<EventContractIdentityAssignment> PlanEvents(
        SemanticIdentityCatalog previous,
        ImmutableArray<SemanticAddress> current,
        ImmutableArray<EventContractIdentityRename> renames)
    {
        var assignments = current.Select(address =>
        {
            var unchanged = previous.EventContracts.FirstOrDefault(_ => _.Address.Equals(address));
            if (unchanged is not null)
            {
                return unchanged;
            }

            var rename = renames.FirstOrDefault(_ => _.CurrentAddress.Equals(address));
            if (rename is not null)
            {
                var assignment = previous.EventContracts.Single(_ => _.Address.Equals(rename.PreviousAddress));
                return assignment with { Address = address, Origin = SemanticIdentityOrigin.Persisted };
            }

            return new EventContractIdentityAssignment(
                address,
                EventContractId.CreateLegacy(previous.Application, address.Name),
                EventContractRevision.Initial,
                SemanticIdentityOrigin.LegacyBootstrap);
        }).OrderBy(_ => _.Id.ToString(), StringComparer.Ordinal);
        return [.. assignments];
    }

    static void ValidateCurrent(
        ApplicationIdentity application,
        ImmutableArray<string> documentKeys,
        ImmutableArray<SemanticAddress> semanticAddresses,
        ImmutableArray<SemanticAddress> eventAddresses)
    {
        if (semanticAddresses.Any(_ => _ is null || _.Application != application) ||
            eventAddresses.Any(_ => _ is null || _.Application != application || _.Kind != SemanticKind.EventContract))
        {
            throw new InvalidSemanticContract("Current identity addresses are malformed or belong to another application.");
        }

        RejectDuplicates(documentKeys, StringComparer.Ordinal, "current document key");
        RejectDuplicates(semanticAddresses, EqualityComparer<SemanticAddress>.Default, "current semantic address");
        RejectDuplicates(eventAddresses, EqualityComparer<SemanticAddress>.Default, "current event address");
    }

    static void ValidateRenames(
        SemanticIdentityCatalog previous,
        ImmutableArray<string> currentKeys,
        ImmutableArray<SemanticAddress> currentSemantics,
        ImmutableArray<SemanticAddress> currentEvents,
        ImmutableArray<DocumentIdentityRename> documentRenames,
        ImmutableArray<SemanticIdentityRename> semanticRenames,
        ImmutableArray<EventContractIdentityRename> eventRenames)
    {
        ValidateSemanticRenameEndpoints(semanticRenames);
        ValidateEventRenameEndpoints(eventRenames);
        ValidateOneToOne(documentRenames, _ => _.PreviousKey, _ => _.CurrentKey, StringComparer.Ordinal, "document rename");
        ValidateOneToOne(semanticRenames, _ => _.PreviousAddress, _ => _.CurrentAddress, EqualityComparer<SemanticAddress>.Default, "semantic rename");
        ValidateOneToOne(eventRenames, _ => _.PreviousAddress, _ => _.CurrentAddress, EqualityComparer<SemanticAddress>.Default, "event rename");

        if (documentRenames.Any(_ => _.PreviousKey == _.CurrentKey || !previous.Documents.Any(a => a.Key == _.PreviousKey) ||
                                    previous.Documents.Any(a => a.Key == _.CurrentKey) || !currentKeys.Contains(_.CurrentKey) || currentKeys.Contains(_.PreviousKey)) ||
            semanticRenames.Any(_ => _.PreviousAddress.Equals(_.CurrentAddress) || !previous.Semantics.Any(a => a.Address.Equals(_.PreviousAddress)) ||
                                     previous.Semantics.Any(a => a.Address.Equals(_.CurrentAddress)) || !currentSemantics.Contains(_.CurrentAddress) || currentSemantics.Contains(_.PreviousAddress)) ||
            eventRenames.Any(_ => _.PreviousAddress.Equals(_.CurrentAddress) || !previous.EventContracts.Any(a => a.Address.Equals(_.PreviousAddress)) ||
                                  previous.EventContracts.Any(a => a.Address.Equals(_.CurrentAddress)) || !currentEvents.Contains(_.CurrentAddress) || currentEvents.Contains(_.PreviousAddress)))
        {
            throw new InvalidSemanticContract("An identity rename is stale, guessed, or does not map one removed assignment to one new address.");
        }

        var renamedDocumentSources = documentRenames.Select(_ => _.PreviousKey).ToHashSet(StringComparer.Ordinal);
        var renamedSemanticSources = semanticRenames.Select(_ => _.PreviousAddress).ToHashSet();
        var renamedEventSources = eventRenames.Select(_ => _.PreviousAddress).ToHashSet();
        if (previous.Documents.Any(_ => !currentKeys.Contains(_.Key) && !renamedDocumentSources.Contains(_.Key)) ||
            previous.Semantics.Any(_ => !currentSemantics.Contains(_.Address) && !renamedSemanticSources.Contains(_.Address)) ||
            previous.EventContracts.Any(_ => !currentEvents.Contains(_.Address) && !renamedEventSources.Contains(_.Address)))
        {
            throw new InvalidSemanticContract("A base catalog assignment is stale and has no explicit one-to-one rename.");
        }
    }

    static void ValidateSemanticRenameEndpoints(ImmutableArray<SemanticIdentityRename> renames)
    {
        if (renames.Any(_ => _ is null || _.PreviousAddress is null || _.CurrentAddress is null))
        {
            throw new InvalidSemanticContract("A semantic identity rename and both endpoints must be non-null.");
        }

        if (renames.Any(_ => _.PreviousAddress.Kind != _.CurrentAddress.Kind))
        {
            throw new InvalidSemanticContract("A semantic identity rename must preserve the semantic kind.");
        }
    }

    static void ValidateEventRenameEndpoints(ImmutableArray<EventContractIdentityRename> renames)
    {
        if (renames.Any(_ => _ is null || _.PreviousAddress is null || _.CurrentAddress is null))
        {
            throw new InvalidSemanticContract("An event contract identity rename and both endpoints must be non-null.");
        }

        if (renames.Any(_ => _.PreviousAddress.Kind != SemanticKind.EventContract || _.CurrentAddress.Kind != SemanticKind.EventContract))
        {
            throw new InvalidSemanticContract("An event contract identity rename requires event contract endpoints.");
        }
    }

    static void ValidateOneToOne<T, TKey>(
        ImmutableArray<T> renames,
        Func<T, TKey> previous,
        Func<T, TKey> current,
        IEqualityComparer<TKey> comparer,
        string description)
    {
        if (renames.Any(_ => _ is null))
        {
            throw new InvalidSemanticContract($"A {description} cannot be null.");
        }

        RejectDuplicates(renames.Select(previous), comparer, $"{description} source");
        RejectDuplicates(renames.Select(current), comparer, $"{description} target");
    }

    static DocumentIdentityAssignment Normalize(DocumentIdentityAssignment assignment)
    {
        if (assignment is null)
        {
            throw new InvalidSemanticContract("A document identity assignment cannot be null.");
        }

        return assignment with { Key = NormalizeKey(assignment.Key) };
    }

    static DocumentIdentityRename Normalize(DocumentIdentityRename rename)
    {
        if (rename is null)
        {
            throw new InvalidSemanticContract("A document identity rename cannot be null.");
        }

        return rename with { PreviousKey = NormalizeKey(rename.PreviousKey), CurrentKey = NormalizeKey(rename.CurrentKey) };
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
            if (!assignment.Id.IsSet ||
                (assignment.Origin == SemanticIdentityOrigin.LegacyBootstrap && assignment.Id != DocumentId.Create(assignment.Key)))
            {
                throw new InvalidSemanticContract($"Document identity assignment '{assignment.Key}' is malformed or has a mismatched legacy bootstrap identity.");
            }
        }

        RejectDuplicates(assignments.Select(_ => _.Key), StringComparer.Ordinal, "document assignment key");
        RejectDuplicates(assignments.Select(_ => _.Id), EqualityComparer<DocumentId>.Default, "document identity");
    }

    static void ValidateSemanticAssignments(ApplicationIdentity application, ImmutableArray<SemanticIdentityAssignment> assignments)
    {
        foreach (var assignment in assignments)
        {
            if (assignment is null || assignment.Address is null || assignment.Address.Application != application || !assignment.Id.IsSet)
            {
                throw new InvalidSemanticContract("A semantic identity assignment is malformed or belongs to another application.");
            }

            ValidateOrigin(assignment.Origin);
            if (assignment.Origin == SemanticIdentityOrigin.LegacyBootstrap && assignment.Id != SemanticId.Create(assignment.Address))
            {
                throw new InvalidSemanticContract($"Semantic assignment '{assignment.Id}' does not match its deterministic legacy bootstrap identity.");
            }
        }

        RejectDuplicates(assignments.Select(_ => _.Address), EqualityComparer<SemanticAddress>.Default, "semantic assignment address");
        RejectDuplicates(assignments.Select(_ => _.Id), EqualityComparer<SemanticId>.Default, "semantic identity");
    }

    static void ValidateEventAssignments(ApplicationIdentity application, ImmutableArray<EventContractIdentityAssignment> assignments)
    {
        foreach (var assignment in assignments)
        {
            if (assignment is null || assignment.Address is null || assignment.Address.Application != application ||
                assignment.Address.Kind != SemanticKind.EventContract || !assignment.Id.IsSet || assignment.Revision != EventContractRevision.Initial)
            {
                throw new InvalidSemanticContract("An event contract identity assignment is malformed or belongs to another application.");
            }

            ValidateOrigin(assignment.Origin);
            if (assignment.Origin == SemanticIdentityOrigin.LegacyBootstrap &&
                assignment.Id != EventContractId.CreateLegacy(application, assignment.Address.Name))
            {
                throw new InvalidSemanticContract($"Event contract assignment '{assignment.Id}' does not match its deterministic legacy bootstrap identity.");
            }
        }

        RejectDuplicates(assignments.Select(_ => _.Address), EqualityComparer<SemanticAddress>.Default, "event assignment address");
        ValidateEffectiveEventContracts(assignments);
    }

    static void ValidateEffectiveEventContracts(IEnumerable<EventContractIdentityAssignment> assignments)
    {
        var collision = assignments
            .GroupBy(_ => _.Id)
            .Where(_ => _.Count() > 1)
            .OrderBy(_ => _.Key.ToString(), StringComparer.Ordinal)
            .FirstOrDefault();
        if (collision is null)
        {
            return;
        }

        var addresses = string.Join(", ", collision.Select(_ => DescribeAddress(_.Address)).Order(StringComparer.Ordinal));
        throw new InvalidSemanticContract($"Effective event contract identity '{collision.Key}' is ambiguous across current event addresses: {addresses}.");
    }

    static string DescribeAddress(SemanticAddress address) =>
        $"{address.Kind}[{string.Join('/', address.Parts.Select(_ => $"{(int)_.Kind}:{_.Key.Length}:{_.Key}"))}]";

    static void RequireApplicationAddress(ApplicationIdentity application, SemanticAddress address)
    {
        if (address is null || address.Application != application)
        {
            throw new InvalidSemanticContract("Identity resolution requires an address in the catalog application.");
        }
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
