// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.CanonicalCorpus;

/// <summary>
/// Represents a minor-version golden without changing the historical integer-version vector API.
/// </summary>
/// <param name="Version">The exact ESM version pair.</param>
/// <param name="Bytes">The immutable canonical wire bytes.</param>
/// <param name="SemanticRevision">The revision verified by the strict reader.</param>
public sealed record EsmMinorGoldenVector(SemanticVersion Version, ImmutableArray<byte> Bytes, SemanticRevision SemanticRevision);
