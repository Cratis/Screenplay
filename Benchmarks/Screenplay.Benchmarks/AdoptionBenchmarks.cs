// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using System.Buffers.Text;
using System.Collections.Frozen;
using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;

namespace Cratis.Screenplay.Benchmarks;

/// <summary>
/// Compares each .NET 9/10 API adopted by Step 3 against the code it replaced, so a change that does not move a number can be dropped.
/// </summary>
[MemoryDiagnoser]
public class AdoptionBenchmarks
{
    static readonly string[] _words = ["authorize", "produces", "reads", "returns", "description", "name", "amount", "status"];
    static readonly HashSet<string> _hashSet = new(["description", "file", "produces", "invokes", "reads"], StringComparer.Ordinal);
    static readonly FrozenSet<string> _frozenSet = _hashSet.ToFrozenSet(StringComparer.Ordinal);
    static readonly SearchValues<char> _glob = SearchValues.Create("*?[]{}");
    static readonly string[] _paths = ["Features/Billing/Invoices/invoice.play", "Modules/Orders/order-lifecycle.play", "Shared/Kernel/concepts.play"];

    /// <summary>Allocates a note identity the way it was done before.</summary>
    /// <returns>The identity.</returns>
    [Benchmark(Baseline = true)]
    public string NoteId_Base64Replace() =>
        $"note_{Convert.ToBase64String(RandomNumberGenerator.GetBytes(16)).TrimEnd('=').Replace('+', '-').Replace('/', '_')}";

    /// <summary>Allocates a note identity with <see cref="Base64Url"/>.</summary>
    /// <returns>The identity.</returns>
    [Benchmark]
    public string NoteId_BclEncoder() => $"note_{Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(16))}";

    /// <summary>Looks words up in a <see cref="HashSet{T}"/>.</summary>
    /// <returns>The hit count.</returns>
    [Benchmark]
    public int Reserved_HashSet()
    {
        var hits = 0;
        foreach (var word in _words)
        {
            if (_hashSet.Contains(word)) hits++;
        }

        return hits;
    }

    /// <summary>Looks words up in a <see cref="FrozenSet{T}"/>.</summary>
    /// <returns>The hit count.</returns>
    [Benchmark]
    public int Reserved_FrozenSet()
    {
        var hits = 0;
        foreach (var word in _words)
        {
            if (_frozenSet.Contains(word)) hits++;
        }

        return hits;
    }

    /// <summary>Scans paths for glob characters with a collection-expression span.</summary>
    /// <returns>The hit count.</returns>
    [Benchmark]
    public int Glob_CollectionExpression()
    {
        var hits = 0;
        foreach (var path in _paths)
        {
            if (path.IndexOfAny(['*', '?', '[', ']', '{', '}']) >= 0) hits++;
        }

        return hits;
    }

    /// <summary>Scans paths for glob characters with <see cref="SearchValues{T}"/>.</summary>
    /// <returns>The hit count.</returns>
    [Benchmark]
    public int Glob_SearchValues()
    {
        var hits = 0;
        foreach (var path in _paths)
        {
            if (path.AsSpan().IndexOfAny(_glob) >= 0) hits++;
        }

        return hits;
    }
}
