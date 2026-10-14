// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces;

/// <summary>
/// The persisted identity of one <c>.play</c> document in a model root.
/// </summary>
/// <param name="Id">The stable document identity.</param>
/// <param name="StableKey">The opaque stable key retained across moves.</param>
/// <param name="Path">The root-relative portable path the document is mapped to.</param>
public sealed record PersistedDocumentMapping(DocumentId Id, string StableKey, PortablePlayPath Path);
