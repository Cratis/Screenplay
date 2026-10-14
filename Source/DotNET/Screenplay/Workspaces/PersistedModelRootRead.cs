// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces;

/// <summary>
/// One read of a model root: the opened workspace plus the exact sidecar bytes it was opened from.
/// </summary>
/// <param name="Workspace">The workspace opened with the persisted identity catalog.</param>
/// <param name="Identities">The persisted identity state read from <paramref name="State"/>.</param>
/// <param name="State">The exact <c>.screenplay/identities.json</c> bytes.</param>
/// <param name="BaselineBytes">The exact <c>.screenplay/release-baseline.json</c> bytes, or null when absent.</param>
/// <param name="Baseline">The validated release-baseline read of <paramref name="BaselineBytes"/>.</param>
sealed record PersistedModelRootRead(ScreenplayWorkspace Workspace, PersistedIdentityState Identities, byte[] State, byte[]? BaselineBytes, ReleaseBaselineRead Baseline);
