// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.CanonicalCorpus;

/// <summary>
/// The exception that is thrown when a requested full ESM golden vector is not embedded in the corpus package.
/// </summary>
/// <param name="version">The requested ESM version.</param>
public sealed class EsmGoldenVectorNotFound(int version) : Exception($"No full ESM golden vector is available for version {version}.");
