// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Represents a portable high-entropy generation strategy (ESM v15).
/// </summary>
/// <param name="Kind">The closed strategy name.</param>
public sealed record SemanticGeneration(string Kind)
{
    /// <summary>
    /// Gets the declared prefix, including the TypeID argument.
    /// </summary>
    public string? Prefix { get; init; }

    /// <summary>
    /// Gets the explicitly declared letter case.
    /// </summary>
    public string? Case { get; init; }

    /// <summary>
    /// Gets the NanoID size (21 by default).
    /// </summary>
    public int? Size { get; init; }

    /// <summary>
    /// Gets the non-default NanoID alphabet.
    /// </summary>
    public string? Alphabet { get; init; }

    /// <summary>
    /// Gets the random byte count.
    /// </summary>
    public int? Count { get; init; }

    /// <summary>
    /// Gets the random byte encoding.
    /// </summary>
    public string? Encoding { get; init; }

    /// <summary>
    /// Gets the finite generation pattern (v21).
    /// </summary>
    public string? Pattern { get; init; }

    /// <summary>
    /// Gets the closed checksum algorithm applied to the pattern (v21).
    /// </summary>
    public string? Checksum { get; init; }

    /// <summary>
    /// Gets the inclusive Int64 lower bound (v21).
    /// </summary>
    public long? Minimum { get; init; }

    /// <summary>
    /// Gets the inclusive Int64 upper bound (v21).
    /// </summary>
    public long? Maximum { get; init; }

    /// <summary>
    /// Gets the ValueGeneration implementation requirement identity (v21).
    /// </summary>
    public string? RequirementId { get; init; }
}
