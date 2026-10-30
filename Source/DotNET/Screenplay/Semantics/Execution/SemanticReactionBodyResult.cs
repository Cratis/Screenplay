// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution;

/// <summary>
/// Result version 1: Empty, an ordered event batch or an ordered command batch, never mixed.
/// Missing results and default arrays are Unsupported, not Empty. The entire batch is validated before settlement.
/// </summary>
public sealed record SemanticReactionBodyResult
{
    /// <summary>Gets the event payloads, or default when absent.</summary>
    public ImmutableArray<SemanticReactionEffect> Events { get; init; }

    /// <summary>Gets the command payloads, or default when absent.</summary>
    public ImmutableArray<SemanticReactionEffect> Commands { get; init; }

    /// <summary>Gets an explicit Unsupported diagnostic, if supplied.</summary>
    public string? Diagnostic { get; init; }

    /// <summary>Gets an explicit successful result with no effects.</summary>
    public static SemanticReactionBodyResult Empty { get; } = new() { Events = [], Commands = [] };

    /// <summary>Answers an ordered event batch.</summary>
    /// <param name="events">The event payloads.</param>
    /// <returns>The event result.</returns>
    public static SemanticReactionBodyResult Produced(ImmutableArray<SemanticReactionEffect> events) => new() { Events = events };

    /// <summary>Answers an ordered command batch.</summary>
    /// <param name="commands">The command payloads.</param>
    /// <returns>The command result.</returns>
    public static SemanticReactionBodyResult Invoked(ImmutableArray<SemanticReactionEffect> commands) => new() { Commands = commands };

    /// <summary>Declines execution explicitly rather than answering Empty.</summary>
    /// <param name="diagnostic">The host-owned explanation.</param>
    /// <returns>An unsupported result.</returns>
    public static SemanticReactionBodyResult Unsupported(string diagnostic) => new() { Diagnostic = diagnostic };
}
