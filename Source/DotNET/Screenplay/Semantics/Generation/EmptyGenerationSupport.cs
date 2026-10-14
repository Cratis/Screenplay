// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Screenplay.Semantics.Generation;

/// <summary>
/// The exception that is thrown when a generation pattern has no admissible values.
/// </summary>
/// <param name="date">The offending UTC date, if the pattern uses date segments.</param>
/// <param name="checksum">The conditioning checksum.</param>
public sealed class EmptyGenerationSupport(DateOnly? date, string? checksum) : Exception($"Generation support is empty{(date is null ? string.Empty : " on UTC " + date.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))} with checksum '{checksum ?? "none"}'.");
