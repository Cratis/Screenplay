// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Cratis.Screenplay.Semantics.Execution;

/// <summary>
/// Describes the protected read-model state a command decision depended on, as keys paired with the event contracts that change them.
/// </summary>
/// <remarks>
/// Two instances are equal when their <see cref="Entries"/> are equal pairwise in order. Capture builds the entries in the order the
/// command first reads each distinct key, so the order is canonical for one command and one request. Entries compare their keys with the
/// ordinal semantic value rules and their event contracts as sets.
/// </remarks>
public sealed record SemanticDecisionDependencies
{
    internal SemanticDecisionDependencies(ImmutableArray<Entry> entries) => Entries = entries;

    /// <summary>
    /// Gets the dependencies of a decision that read no protected state; nothing can conflict with it.
    /// </summary>
    public static SemanticDecisionDependencies Empty { get; } = new([]);

    /// <summary>
    /// Gets one entry per distinct protected key, merged across every read of that key.
    /// </summary>
    public ImmutableArray<Entry> Entries { get; }

    /// <summary>
    /// Determines whether another instance holds equal entries in the same order.
    /// </summary>
    /// <param name="other">The instance to compare with.</param>
    /// <returns><see langword="true"/> when the entries are equal in order.</returns>
    public bool Equals(SemanticDecisionDependencies? other) =>
        other is not null && (ReferenceEquals(this, other) || Entries.SequenceEqual(other.Entries));

    /// <inheritdoc/>
    public override int GetHashCode() => Entries.Aggregate(0, (hash, entry) => HashCode.Combine(hash, entry));

    /// <summary>
    /// Determines whether a later fact changes state this decision depended on.
    /// </summary>
    /// <remarks>
    /// A fact matches when its event contract is in an entry's set and its destination equals that entry's key. Key comparison is ordinal, matching projection instance keying, including UUID text.
    /// </remarks>
    /// <param name="fact">The fact after the decision boundary.</param>
    /// <returns><see langword="true"/> when the fact conflicts with the decision.</returns>
    public bool Matches(SemanticFact fact) => Entries.Any(entry =>
        entry.EventContracts.Contains(fact.EventContract) && SemanticValueRules.AreEqual(entry.Key, fact.Destination));

    internal static SemanticDecisionDependencies Capture(
        SemanticExecutionPlan plan,
        SemanticCommand command,
        IReadOnlyDictionary<SemanticId, SemanticValue> values)
    {
        var entries = new List<Entry>();
        foreach (var read in command.Reads)
        {
            var key = values[read.Key];
            var handled = plan.Projections.Values
                .Where(projection => projection.Target == SemanticProjectionTargetKind.ReadModel && projection.ReadModel == read.ReadModel && projection.Scope is not null)
                .SelectMany(projection => projection.Scope!.From.Select(from => from.EventContract)
                    .Concat(projection.Scope.Removals.Select(removal => removal.EventContract)))
                .ToImmutableHashSet();
            var index = entries.FindIndex(entry => SemanticValueRules.AreEqual(entry.Key, key));
            if (index < 0) entries.Add(new(key, handled));
            else entries[index] = entries[index] with { EventContracts = entries[index].EventContracts.Union(handled) };
        }

        return new([.. entries]);
    }

    /// <summary>
    /// Represents one protected key and the event contracts that change the read models behind it.
    /// </summary>
    /// <remarks>
    /// Entries are equal when their keys are equal by the ordinal semantic value rules and their event contracts are equal as sets.
    /// </remarks>
    /// <param name="Key">The key value the command read by.</param>
    /// <param name="EventContracts">The event contracts the reading views handle or remove with, merged per key.</param>
    [SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "Entry is only meaningful as an element of its dependencies (decision 0089).")]
    public sealed record Entry(SemanticValue Key, ImmutableHashSet<SemanticId> EventContracts)
    {
        /// <summary>
        /// Determines whether another entry has an equal key and the same event contracts.
        /// </summary>
        /// <param name="other">The entry to compare with.</param>
        /// <returns><see langword="true"/> when the entries are equal.</returns>
        public bool Equals(Entry? other) =>
            other is not null && (ReferenceEquals(this, other) ||
                (SemanticValueRules.AreEqual(Key, other.Key) && EventContracts.SetEquals(other.EventContracts)));

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(
            Key switch
            {
                SemanticTextValue text => text.Value.GetHashCode(StringComparison.Ordinal),
                SemanticNumberValue number => number.Value.GetHashCode(),
                SemanticBooleanValue boolean => boolean.Value.GetHashCode(),
                _ => (int)Key.Kind
            },
            EventContracts.Aggregate(0, (hash, contract) => hash ^ contract.GetHashCode()));
    }
}
