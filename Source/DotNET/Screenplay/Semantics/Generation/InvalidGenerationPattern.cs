// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Generation;

/// <summary>
/// The exception that is thrown when a finite generation pattern is malformed.
/// </summary>
/// <param name="reason">The actionable grammar failure.</param>
/// <param name="offset">The zero-based offset in the pattern.</param>
public sealed class InvalidGenerationPattern(string reason, int offset) : Exception($"{reason} at pattern offset {offset}.")
{
    /// <summary>
    /// Gets the zero-based offset in the pattern.
    /// </summary>
    public int Offset { get; } = offset;
}
