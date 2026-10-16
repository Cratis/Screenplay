// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Cratis.Screenplay.Semantics.Execution;

/// <summary>
/// Describes the protected read-model state a command decision depended on, as keys paired with the event contracts that change them.
/// </summary>
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
    /// <param name="Key">The key value the command read by.</param>
    /// <param name="EventContracts">The event contracts the reading views handle or remove with, merged per key.</param>
    [SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "Entry is only meaningful as an element of its dependencies (decision 0089).")]
    public sealed record Entry(SemanticValue Key, ImmutableHashSet<SemanticId> EventContracts);
}
