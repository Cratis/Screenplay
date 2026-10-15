// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution;

internal sealed record SemanticDecisionDependencies(ImmutableArray<SemanticDecisionDependencies.Entry> Entries)
{
    internal static SemanticDecisionDependencies Empty { get; } = new([]);

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
            else entries[index] = entries[index] with { Handled = entries[index].Handled.Union(handled) };
        }

        return new([.. entries]);
    }

    internal bool Matches(SemanticFact fact) => Entries.Any(entry =>
        entry.Handled.Contains(fact.EventContract) && SemanticValueRules.AreEqual(entry.Key, fact.Destination));

    internal sealed record Entry(SemanticValue Key, ImmutableHashSet<SemanticId> Handled);
}
