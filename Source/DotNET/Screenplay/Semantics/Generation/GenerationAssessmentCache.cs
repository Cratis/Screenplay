// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Numerics;

namespace Cratis.Screenplay.Semantics.Generation;

// Successful assessments only, bounded by entry count and authored-key length. Nothing serializes.
internal sealed class GenerationAssessmentCache(int capacity = 128)
{
#if NET9_0_OR_GREATER
    readonly Lock _gate = new();
#else
    readonly object _gate = new();
#endif
    readonly Dictionary<Key, (Lazy<BigInteger> Value, LinkedListNode<Key> Node)> _entries = [];
    readonly LinkedList<Key> _order = new();

    internal static GenerationAssessmentCache Shared { get; } = new();

    internal BigInteger Get(string pattern, string? checksum, int year, Func<BigInteger> assess)
    {
        if (capacity < 1) throw new InvalidSemanticContract("Assessment cache capacity must be positive.");
        if (pattern.Length > 4096) return assess();
        var key = new Key(pattern, checksum, year);
        Lazy<BigInteger> value;
        lock (_gate)
        {
            if (_entries.TryGetValue(key, out var entry))
            {
                value = entry.Value;
            }
            else
            {
                if (_entries.Count == capacity)
                {
                    _entries.Remove(_order.First!.Value);
                    _order.RemoveFirst();
                }
                value = new(assess);
                _entries.Add(key, (value, _order.AddLast(key)));
            }
        }
        try
        {
            return value.Value;
        }
        catch
        {
            // A failure must be re-assessed, never reused as admission or a stale refusal.
            lock (_gate)
            {
                if (_entries.TryGetValue(key, out var entry) && ReferenceEquals(entry.Value, value))
                {
                    _entries.Remove(key);
                    _order.Remove(entry.Node);
                }
            }
            throw;
        }
    }

    sealed record Key(string Pattern, string? Checksum, int Year);
}
