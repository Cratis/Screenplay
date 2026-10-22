// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Matching;

// Sparse-set membership makes every instruction visit once per input position, even in epsilon cycles.
internal sealed class MatchStateSet(int capacity)
{
    readonly int[] _dense = new int[capacity];
    readonly int[] _sparse = new int[capacity];

    internal int Count { get; private set; }

    internal int this[int index] => _dense[index];

    internal void Clear() => Count = 0;

    internal void Add(int state)
    {
        if (state < 0 || (_sparse[state] < Count && _dense[_sparse[state]] == state)) return;
        _sparse[state] = Count;
        _dense[Count++] = state;
    }
}
