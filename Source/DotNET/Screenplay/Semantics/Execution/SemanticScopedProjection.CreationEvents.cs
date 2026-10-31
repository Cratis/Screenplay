// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace Cratis.Screenplay.Semantics.Execution;

/// <summary>
/// Resolves shared child events through their earlier creation event, following Chronicle's FindParentEvent.
/// </summary>
internal sealed partial class SemanticScopedProjection
{
    static IEnumerable<SemanticId> OwnEventContracts(SemanticProjectionScope scope) =>
        scope.From.Select(_ => _.EventContract)
            .Concat(scope.Joins.Select(_ => _.EventContract))
            .Concat(scope.Removals.Select(_ => _.EventContract))
            .Concat(scope.JoinRemovals.Select(_ => _.EventContract));

    static (SemanticProjectionKey? Key, SemanticProjectionKey? ParentKey) CreationKeys(SemanticProjectionScope scope, SemanticId contract)
    {
        if (FirstWithContract(scope.From, static _ => _.EventContract, contract) is { } from)
        {
            return (from.Key, from.ParentKey);
        }

        if (FirstWithContract(scope.Removals, static _ => _.EventContract, contract) is { } removal)
        {
            return (removal.Key, removal.ParentKey);
        }

        return (null, null);
    }

    static T? FirstWithContract<T>(ImmutableArray<T> items, Func<T, SemanticId> contractOf, SemanticId contract)
        where T : class
    {
        foreach (var item in items)
        {
            if (contractOf(item) == contract)
            {
                return item;
            }
        }

        return null;
    }

    bool SharesEventWithParent(Level level)
    {
        // Avoid a capturing predicate on the ordinary, non-shared child path.
        foreach (var from in level.Ancestors[^1].Scope.From)
        {
            if (from.EventContract == _fact.EventContract)
            {
                return true;
            }
        }

        return false;
    }

    bool IsCreationContract(HashSet<SemanticId> contracts, SemanticId contract) =>
        contract != _fact.EventContract && contracts.Contains(contract);

    SemanticFact? FindCreationEvent(SemanticProjectionScope scope, SemanticValue identity)
    {
        var contracts = OwnedContractCache.Get(scope);
        var index = CreationEventIndex.For(observed);
        index.Observe(observed);
        if (index.CanLookup(identity))
        {
            return index.Find(contracts, _fact.EventContract, identity);
        }

        // Malformed composites with duplicate properties make AreEqual asymmetric. They (and unknown
        // variants) cannot be dictionary keys with that equality; preserve the original scan for such runs.
        for (var position = 0; position < observed.Count; position++)
        {
            var fact = observed[position];
            if (IsCreationContract(contracts, fact.EventContract) && SemanticValueRules.AreEqual(SourceOf(fact), identity))
            {
                return fact;
            }
        }

        return null;
    }

    // FindChildCreationEvent takes the first earlier event of another own type whose event source is the child key
    // (Chronicle KeyResolvers.cs:586-635). A failed historical resolution falls back without retaining its failure.
    Location? LocateViaCreationEvent(SemanticProjectionScope scope, Level level, SemanticValue identity)
    {
        if (!SharesEventWithParent(level))
        {
            return null;
        }

        if (FindCreationEvent(scope, identity) is not { } creation)
        {
            return null;
        }

        var (key, parentKey) = CreationKeys(scope, creation.EventContract);
        if (key is null)
        {
            return null;
        }

        var current = _fact;
        var failure = _failure;
        try
        {
            _fact = creation;
            if (Key(key) is not { } childIdentity || ParentIdentity(parentKey, level) is not { } parent)
            {
                return null;
            }

            return LocateChild(level, childIdentity, parent);
        }
        finally
        {
            _fact = current;
            _failure = failure;
        }
    }

    // Scope records are immutable. Weak keys keep cached sets alive only as long as their scopes, across per-fact
    // projector instances and concurrent evaluator runs. This holder is reached only for a shared event.
    static class OwnedContractCache
    {
        static readonly ConditionalWeakTable<SemanticProjectionScope, HashSet<SemanticId>> _contracts = [];

        internal static HashSet<SemanticId> Get(SemanticProjectionScope scope) =>
            _contracts.GetValue(scope, static candidate => [.. OwnEventContracts(candidate)]);
    }

    // TryProject owns a fresh, append-only observed list per run. Projectors are per fact, so weakly key
    // the lazy index by that list, not by the plan/scope. Catch up before every lookup, including deferred replay.
    sealed class CreationEventIndex
    {
        static readonly ConditionalWeakTable<IReadOnlyList<SemanticFact>, CreationEventIndex> _runs = [];
        readonly Dictionary<(SemanticId Contract, SemanticValue Source), (int Position, SemanticFact Fact)> _first = new(new CreationSourceComparer());
        int _observedCount;
        bool _requiresScan;

        internal static CreationEventIndex For(IReadOnlyList<SemanticFact> facts) =>
            _runs.GetValue(facts, static _ => new());

        internal void Observe(IReadOnlyList<SemanticFact> facts)
        {
            for (; _observedCount < facts.Count; _observedCount++)
            {
                var fact = facts[_observedCount];
                var source = SourceOf(fact);
                if (!CreationSourceComparer.CanIndex(source))
                {
                    _requiresScan = true;
                    continue;
                }

                _first.TryAdd((fact.EventContract, source), (_observedCount, fact));
            }
        }

        internal bool CanLookup(SemanticValue source) => !_requiresScan && CreationSourceComparer.CanIndex(source);

        internal SemanticFact? Find(HashSet<SemanticId> contracts, SemanticId current, SemanticValue source)
        {
            var earliest = int.MaxValue;
            SemanticFact? first = null;
            foreach (var contract in contracts)
            {
                if (contract == current)
                {
                    continue;
                }

                if (_first.TryGetValue((contract, source), out var candidate) && candidate.Position < earliest)
                {
                    earliest = candidate.Position;
                    first = candidate.Fact;
                }
            }

            return first;
        }
    }

    sealed class CreationSourceComparer : IEqualityComparer<(SemanticId Contract, SemanticValue Source)>
    {
        interface IElementMatch<T>
        {
            static abstract bool Matches(ImmutableArray<T> candidates, int index, T value);
        }

        public bool Equals((SemanticId Contract, SemanticValue Source) x, (SemanticId Contract, SemanticValue Source) y) =>
            x.Contract == y.Contract && ValuesEqual(x.Source, y.Source);

        public int GetHashCode((SemanticId Contract, SemanticValue Source) obj) =>
            HashCode.Combine(obj.Contract, ValueHash(obj.Source));

        internal static bool CanIndex(SemanticValue value) => value switch
        {
            SemanticNullValue or SemanticTextValue or SemanticNumberValue or SemanticBooleanValue => true,
            SemanticArrayValue array => CanIndexArray(array),
            SemanticCompositeValue composite => CanIndexComposite(composite),
            _ => false
        };

        static bool CanIndexArray(SemanticArrayValue array)
        {
            foreach (var value in array.Values)
            {
                if (!CanIndex(value))
                {
                    return false;
                }
            }

            return true;
        }

        // Keep the same value rules without AreEqual's LINQ/closure allocations on dictionary probes.
        static bool ValuesEqual(SemanticValue left, SemanticValue right) => (left, right) switch
        {
            (SemanticNullValue, SemanticNullValue) => true,
            (SemanticTextValue x, SemanticTextValue y) => string.Equals(x.Value, y.Value, StringComparison.Ordinal),
            (SemanticNumberValue x, SemanticNumberValue y) => x.Value == y.Value,
            (SemanticBooleanValue x, SemanticBooleanValue y) => x.Value == y.Value,
            _ => ContainersEqual(left, right)
        };

        static bool ContainersEqual(SemanticValue left, SemanticValue right) => (left, right) switch
        {
            (SemanticArrayValue x, SemanticArrayValue y) => ArraysEqual(x, y),
            (SemanticCompositeValue x, SemanticCompositeValue y) => CompositesEqual(x, y),
            _ => false
        };

        static bool ArraysEqual(SemanticArrayValue left, SemanticArrayValue right) =>
            AllMatch<SemanticValue, PositionalMatch>(left.Values, right.Values);

        static bool CompositesEqual(SemanticCompositeValue left, SemanticCompositeValue right) =>
            AllMatch<SemanticPropertyValue, UnorderedPropertyMatch>(left.Properties, right.Properties);

        // The matcher is a struct type argument so the per-element check is a direct, inlinable call on the comparer's per-probe path.
        static bool AllMatch<T, TMatch>(ImmutableArray<T> left, ImmutableArray<T> right)
            where TMatch : struct, IElementMatch<T>
        {
            if (left.Length != right.Length)
            {
                return false;
            }

            for (var index = 0; index < left.Length; index++)
            {
                if (!TMatch.Matches(right, index, left[index]))
                {
                    return false;
                }
            }

            return true;
        }

        static bool ContainsProperty(ImmutableArray<SemanticPropertyValue> candidates, SemanticPropertyValue property)
        {
            foreach (var candidate in candidates)
            {
                if (candidate.TargetProperty == property.TargetProperty && ValuesEqual(candidate.Value, property.Value))
                {
                    return true;
                }
            }

            return false;
        }

        static bool CanIndexComposite(SemanticCompositeValue composite)
        {
            for (var index = 0; index < composite.Properties.Length; index++)
            {
                if (!CanIndex(composite.Properties[index].Value) || HasEarlierProperty(composite, index))
                {
                    return false;
                }
            }

            return true;
        }

        static bool HasEarlierProperty(SemanticCompositeValue composite, int index)
        {
            for (var earlier = 0; earlier < index; earlier++)
            {
                if (composite.Properties[earlier].TargetProperty == composite.Properties[index].TargetProperty)
                {
                    return true;
                }
            }

            return false;
        }

        // Match AreEqual: ordinal text, numeric decimal equality (including scale), ordered arrays,
        // and order-independent composite properties. No normalization objects are allocated.
        static int ValueHash(SemanticValue value) => value switch
        {
            SemanticTextValue text => text.Value?.GetHashCode(StringComparison.Ordinal) ?? 0,
            SemanticNumberValue number => number.Value.GetHashCode(),
            SemanticBooleanValue boolean => boolean.Value.GetHashCode(),
            SemanticArrayValue array => ArrayHash(array),
            SemanticCompositeValue composite => CompositeHash(composite),
            _ => 0
        };

        static int ArrayHash(SemanticArrayValue array)
        {
            HashCode hash = default;
            foreach (var value in array.Values)
            {
                hash.Add(ValueHash(value));
            }

            return hash.ToHashCode();
        }

        static int CompositeHash(SemanticCompositeValue composite)
        {
            var hash = 0;
            foreach (var property in composite.Properties)
            {
                hash ^= HashCode.Combine(property.TargetProperty, ValueHash(property.Value));
            }

            return HashCode.Combine(composite.Properties.Length, hash);
        }

        readonly struct PositionalMatch : IElementMatch<SemanticValue>
        {
            public static bool Matches(ImmutableArray<SemanticValue> candidates, int index, SemanticValue value) => ValuesEqual(value, candidates[index]);
        }

        readonly struct UnorderedPropertyMatch : IElementMatch<SemanticPropertyValue>
        {
            public static bool Matches(ImmutableArray<SemanticPropertyValue> candidates, int index, SemanticPropertyValue value) => ContainsProperty(candidates, value);
        }
    }
}
