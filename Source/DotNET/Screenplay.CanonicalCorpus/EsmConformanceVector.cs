// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.CanonicalCorpus;

/// <summary>
/// Represents immutable opt-in reader conformance bytes, not a published ESM version.
/// </summary>
/// <param name="Name">The unique frozen fixture name.</param>
/// <param name="Bytes">The exact UTF-8 bytes, including the full document revision.</param>
/// <param name="ShouldAdmit">Whether the opt-in reader should admit the case within a supported range.</param>
public sealed record EsmConformanceVector(string Name, ImmutableArray<byte> Bytes, bool ShouldAdmit);
