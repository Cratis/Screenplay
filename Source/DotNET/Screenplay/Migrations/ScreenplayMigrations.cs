// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Screenplay.Migrations;

/// <summary>
/// Finds the migration steps and chains them by version.
/// </summary>
public static class ScreenplayMigrations
{
    /// <summary>
    /// Finds the steps by convention: every concrete class in the given assemblies - the compiler's own when none is
    /// given - that implements <see cref="IScreenplayMigration"/> and has a public parameterless constructor.
    /// </summary>
    /// <param name="assemblies">The assemblies to look in.</param>
    /// <returns>The steps, ordered by the version they read.</returns>
    public static IReadOnlyList<IScreenplayMigration> Discover(params Assembly[] assemblies)
    {
        var searched = assemblies.Length == 0 ? [typeof(ScreenplayMigrations).Assembly] : assemblies;
        return [.. searched
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type is { IsClass: true, IsAbstract: false } && typeof(IScreenplayMigration).IsAssignableFrom(type) && type.GetConstructor(Type.EmptyTypes) is not null)
            .Select(type => (IScreenplayMigration)Activator.CreateInstance(type)!)
            .OrderBy(step => step.From.Major)
            .ThenBy(step => step.Name.Value, StringComparer.Ordinal)];
    }

    /// <summary>
    /// Chains the steps that move a document from one version to another.
    /// </summary>
    /// <param name="steps">The steps to chain.</param>
    /// <param name="from">The version the documents are written in.</param>
    /// <param name="to">The version to end at.</param>
    /// <returns>The <see cref="MigrationPlan"/>; it has no steps when the versions are the same.</returns>
    /// <exception cref="InvalidMigrationPlan">A step goes backwards, two steps overlap, <paramref name="to"/> is earlier than
    /// <paramref name="from"/>, or no step covers part of the way.</exception>
    public static MigrationPlan Plan(IEnumerable<IScreenplayMigration> steps, ScreenplayVersion from, ScreenplayVersion to)
    {
        var ordered = steps.OrderBy(step => step.From.Major).ThenBy(step => step.To.Major).ToArray();
        if (Array.Find(ordered, step => step.To <= step.From) is { } backwards)
        {
            throw new InvalidMigrationPlan($"The step '{backwards.Name}' goes from Screenplay {backwards.From} to {backwards.To} - a step moves forward");
        }

        // Two steps that cover the same versions would make the result depend on which one ran; refuse them up front,
        // not only when a plan happens to cross them.
        for (var index = 1; index < ordered.Length; index++)
        {
            var previous = ordered[index - 1];
            var current = ordered[index];
            if (current.From < previous.To)
            {
                throw new InvalidMigrationPlan(
                    $"The steps '{previous.Name}' ({previous.From}→{previous.To}) and '{current.Name}' ({current.From}→{current.To}) overlap - each version is left by one step");
            }
        }

        if (to < from)
        {
            throw new InvalidMigrationPlan($"Cannot migrate from Screenplay {from} back to {to} - migrations only move forward");
        }

        var chain = new List<IScreenplayMigration>();
        var at = from;
        while (at < to)
        {
            var next = Array.Find(ordered, step => step.From == at)
                ?? throw new InvalidMigrationPlan(ordered.Length > 0 && at == from && at < ordered[0].From
                    ? $"No migration step goes from Screenplay {at} - the oldest step starts at Screenplay {ordered[0].From} - a model written for an earlier version migrates from {ordered[0].From}"
                    : $"No migration step goes from Screenplay {at} - cannot migrate from {from} to {to}");

            if (next.To > to)
            {
                throw new InvalidMigrationPlan($"The step '{next.Name}' goes from Screenplay {next.From} to {next.To}, past {to} - cannot stop at {to}");
            }

            chain.Add(next);
            at = next.To;
        }

        return new(from, to, chain);
    }
}
