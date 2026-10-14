// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Cratis.Screenplay.Workspaces;

/// <summary>
/// A model root opened with its persisted identities and release-baseline read, or a typed refusal.
/// </summary>
/// <param name="Workspace">The workspace compiled from the root's source with the persisted identity catalog, or null on refusal.</param>
/// <param name="Identities">The persisted identity state read from <c>.screenplay/identities.json</c>, or null on refusal.</param>
/// <param name="ReleaseBaseline">The absent, valid or unreadable <c>.screenplay/release-baseline.json</c> read, or null on refusal.</param>
/// <param name="FailureKind">
/// The refusal discriminator, or null: <c>PendingOperation</c>, <c>PersistedIdentitiesRequired</c>, <c>IdentityStateConflict</c>,
/// <c>IdentityMappingConflict</c>, <c>IdentityReconciliationRequired</c>, <c>UnresolvedPlacement</c>, <c>MetadataPermissions</c>,
/// <c>DiskDrift</c>, <c>InvalidSemanticContract</c> or <c>UnreadableModelRoot</c>.
/// </param>
/// <param name="Reason">The complete display-ready refusal message, or null.</param>
/// <remarks>
/// An unreadable release baseline is not a refusal: the root still opens and <see cref="ReleaseBaseline"/>
/// carries status <c>unreadable</c>, which a baseline check reports as <c>UnreadableReleaseBaseline</c>.
/// </remarks>
public sealed record ScreenplayModelRootOutcome(
    ScreenplayWorkspace? Workspace,
    PersistedIdentityState? Identities,
    ReleaseBaselineRead? ReleaseBaseline,
    string? FailureKind = null,
    string? Reason = null)
{
    /// <summary>
    /// Gets a value indicating whether the root opened; when true, <see cref="Workspace"/>, <see cref="Identities"/> and <see cref="ReleaseBaseline"/> are set.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Workspace), nameof(Identities), nameof(ReleaseBaseline))]
    [MemberNotNullWhen(false, nameof(FailureKind), nameof(Reason))]
    public bool Success => FailureKind is null;
}
