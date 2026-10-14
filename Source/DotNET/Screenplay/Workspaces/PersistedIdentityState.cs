// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces;

/// <summary>
/// A model root's persisted identities: the content of <c>.screenplay/identities.json</c>.
/// </summary>
/// <param name="ApplicationName">The friendly application name.</param>
/// <param name="IdentityCatalog">The authoritative identity catalog, including assignments retained for removed declarations.</param>
/// <param name="Documents">The document identities and the paths they are mapped to.</param>
public sealed record PersistedIdentityState(string ApplicationName, SemanticIdentityCatalog IdentityCatalog, ImmutableArray<PersistedDocumentMapping> Documents);
