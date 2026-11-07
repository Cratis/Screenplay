// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Migrations;

/// <summary>
/// Represents the steps that move a document from one Screenplay version to another, in the order they run.
/// </summary>
/// <param name="From">The version the documents are written in.</param>
/// <param name="To">The version the documents end at.</param>
/// <param name="Steps">The <see cref="IScreenplayMigration">steps</see>, each starting where the one before it ended.</param>
public sealed record MigrationPlan(ScreenplayVersion From, ScreenplayVersion To, IReadOnlyList<IScreenplayMigration> Steps)
{
    /// <summary>
    /// Gets the plan as it reads in a report - <c>5→6 slots-and-outlets</c>, for example.
    /// </summary>
    public string Description => Steps.Count == 0
        ? "no steps"
        : string.Join(", ", Steps.Select(step => $"{step.From}→{step.To} {step.Name}"));
}
