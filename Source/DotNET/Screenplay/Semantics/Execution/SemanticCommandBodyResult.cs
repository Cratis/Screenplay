// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution;

/// <summary>
/// Supplies result-v1 handler payloads in declared production order, without event descriptors or envelopes.
/// </summary>
/// <remarks>
/// Result v2 supplies selected events through Items and ProducedEvents, preserving the positional
/// result-v1 constructor and Deconstruct contract.
/// </remarks>
/// <param name="Payloads">One semantic composite per unconditional produces line; default means unavailable.</param>
/// <param name="Diagnostic">An optional unsupported diagnostic, invalid on an evaluable batch.</param>
public sealed record SemanticCommandBodyResult(ImmutableArray<SemanticValue> Payloads = default, string? Diagnostic = null)
{
    /// <summary>
    /// Gets the result-v2 items. Default means no v2 result (unavailable); empty means produced nothing.
    /// </summary>
    public ImmutableArray<SemanticCommandBodyItem> Items { get; init; }

    /// <summary>
    /// Creates a batch to validate against the declared produces contract.
    /// </summary>
    /// <param name="payloads">One composite per produces line, in declaration order.</param>
    /// <returns>The supplied batch, subject to typed validation before any facts commit.</returns>
    public static SemanticCommandBodyResult Produced(ImmutableArray<SemanticValue> payloads) => new(payloads);

    /// <summary>
    /// Creates an unevaluable answer, not a rejection or an empty successful batch.
    /// </summary>
    /// <param name="diagnostic">An optional diagnostic.</param>
    /// <returns>An unsupported answer.</returns>
    public static SemanticCommandBodyResult Unsupported(string? diagnostic = null) => new(Diagnostic: diagnostic);

    /// <summary>
    /// Creates an ordered batch of selected possible events, including an empty successful batch.
    /// </summary>
    /// <param name="items">The selected events in production order.</param>
    /// <returns>The supplied result-v2 batch, subject to validation before any facts commit.</returns>
    public static SemanticCommandBodyResult ProducedEvents(ImmutableArray<SemanticCommandBodyItem> items) => new() { Items = items };
}
