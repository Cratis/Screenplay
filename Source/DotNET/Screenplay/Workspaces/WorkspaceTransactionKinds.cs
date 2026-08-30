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
    InvalidIdentityMigration = 5
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
