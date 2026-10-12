// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces;

/// <summary>
/// Defines why an atomic workspace transaction was rejected.
/// </summary>
public enum WorkspaceConflictKind
{
    /// <summary>
    /// An unknown conflict. Unknown values are never emitted.
    /// </summary>
    Unknown = -1,

    /// <summary>
    /// The request was based on an earlier workspace revision.
    /// </summary>
    StaleWorkspaceRevision = 0,

    /// <summary>
    /// The request was based on an earlier identity-catalog revision.
    /// </summary>
    StaleCatalogRevision = 1,

    /// <summary>
    /// An operation is malformed, duplicated, or targets an unknown document.
    /// </summary>
    InvalidOperation = 2,

    /// <summary>
    /// Two documents would occupy paths that a portable file system cannot distinguish.
    /// </summary>
    PortablePathCollision = 3,

    /// <summary>
    /// The candidate source could not parse, merge, bind, or validate as one application.
    /// </summary>
    CompilationFailed = 4,

    /// <summary>
    /// Explicit identity continuity could not be migrated from the requested catalog revision.
    /// </summary>
    InvalidIdentityMigration = 5,

    /// <summary>
    /// A semantic patch operation addressed a semantic identity that does not exist.
    /// </summary>
    SemanticIdNotFound = 6,

    /// <summary>
    /// A semantic patch operation addressed a field that is not admitted by the operation - a non-slice
    /// identity, or a slice with no single-line quoted description to patch.
    /// </summary>
    UnsupportedSemanticField = 7,

    /// <summary>
    /// A semantic patch operation's expected current value did not match the decoded source.
    /// </summary>
    SemanticFieldValueDrift = 8,

    /// <summary>
    /// More than one operation in the same transaction claims the same owning document, at least one of which
    /// is a semantic patch.
    /// </summary>
    MultiOwnerSemanticEdit = 9,

    /// <summary>
    /// Two source files declare the same single-owner artifact.
    /// </summary>
    ConflictingOwner = 10,

    /// <summary>
    /// A diagnostic repair would remove at least one authored comment.
    /// </summary>
    RepairWouldDropComments = 11,

    /// <summary>
    /// A diagnostic repair requires explicit canonical formatting consent.
    /// </summary>
    FormattingConsentRequired = 12,

    /// <summary>
    /// No diagnostic repair recipe matches the original workspace subject.
    /// </summary>
    UnknownRepair = 13,

    /// <summary>
    /// An additional selected property is outside the resolved chain.
    /// </summary>
    HopNotOnChain = 14,

    /// <summary>
    /// Removing a property would leave a reader.
    /// </summary>
    PropertyReadersExist = 15,

    /// <summary>
    /// A selected event contract requires an explicit evolution choice.
    /// </summary>
    EventContractChangeRequiresDecision = 16,

    /// <summary>
    /// A historical event generation cannot be edited.
    /// </summary>
    HistoricalGenerationImmutable = 17,

    /// <summary>
    /// An inline event must first be extracted.
    /// </summary>
    InlineEventRequiresExtraction = 18,

    /// <summary>
    /// An affected property reference is ambiguous or unresolved.
    /// </summary>
    AmbiguousPropertyReference = 19,

    /// <summary>
    /// Opaque or unclassified text prevents a safe property change.
    /// </summary>
    OpaquePropertyReference = 20,

    /// <summary>
    /// The property or reference span is not supported by this planner.
    /// </summary>
    UnsupportedPropertySpan = 21,

    /// <summary>
    /// An added property needs an explicit producer or consumer mapping.
    /// </summary>
    SourcesRequired = 22,

    /// <summary>
    /// An added required property needs a value in existing fixtures or assertions.
    /// </summary>
    FixtureValueRequired = 23
}

/// <summary>
/// Defines how one exact workspace document changes in a successful write plan.
/// </summary>
public enum WorkspaceWriteKind
{
    /// <summary>
    /// An unknown write kind. Unknown values are never emitted.
    /// </summary>
    Unknown = -1,

    /// <summary>
    /// A document is added.
    /// </summary>
    Added = 0,

    /// <summary>
    /// A document's exact bytes are replaced.
    /// </summary>
    Replaced = 1,

    /// <summary>
    /// A document moves to a new path.
    /// </summary>
    Moved = 2,

    /// <summary>
    /// A document's stable key changes while its identity remains the same.
    /// </summary>
    Renamed = 3,

    /// <summary>
    /// A document is removed.
    /// </summary>
    Removed = 4
}
