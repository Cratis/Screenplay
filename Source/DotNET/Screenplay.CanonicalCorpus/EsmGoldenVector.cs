// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.CanonicalCorpus;

/// <summary>
/// Represents exact ESM golden bytes and the semantic revision verified by the strict ESM reader.
/// </summary>
/// <param name="Version">The pinned ESM version.</param>
/// <param name="Bytes">The immutable golden file bytes, including its semantic revision.</param>
/// <param name="SemanticRevision">The semantic revision read from the bytes.</param>
public sealed record EsmGoldenVector(int Version, ImmutableArray<byte> Bytes, SemanticRevision SemanticRevision);
