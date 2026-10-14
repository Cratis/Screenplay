// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.CanonicalCorpus;

/// <summary>
/// Provides the hash-pinned UTC clock schedule boundary vectors checked against the reference clock.
/// </summary>
public static class ClockScheduleVectors
{
    /// <summary>
    /// The clock schedule fixture contract version, independent of the ESM version.
    /// </summary>
    public const int Version = 1;

    /// <summary>
    /// The lowercase SHA-256 of the exact UTF-8 fixture bytes, including the trailing newline.
    /// </summary>
    public const string Sha256 = "e1863c53bcf3e62bc780b31af66f9a189b487db462d156000856d233c0d8e016";

    /// <summary>
    /// Gets the exact immutable bytes of <c>clock-schedules.v1.json</c> without reserialization.
    /// </summary>
    public static ImmutableArray<byte> Bytes { get; } = Read();

    static ImmutableArray<byte> Read()
    {
        using var stream = typeof(ClockScheduleVectors).Assembly.GetManifestResourceStream("Cratis.Screenplay.CanonicalCorpus.Corpus.ClockSchedules.clock-schedules.v1.json") ??
            throw new InvalidSemanticContract("Clock schedule vector resource 'clock-schedules.v1.json' is missing.");
        using var bytes = new MemoryStream();
        stream.CopyTo(bytes);

        return [.. bytes.ToArray()];
    }
}
