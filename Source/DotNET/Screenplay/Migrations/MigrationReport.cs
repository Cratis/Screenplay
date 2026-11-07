// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Migrations;

/// <summary>
/// Represents how a migration of a set of documents went.
/// </summary>
public enum MigrationOutcome
{
    /// <summary>Every document was migrated and compiles with the target version.</summary>
    Migrated = 0,

    /// <summary>A document needs an author: a step left something for review, or the result does not compile.</summary>
    NeedsReview = 2,

    /// <summary>A document could not be migrated at all.</summary>
    Failed = 1
}

/// <summary>
/// Represents what a migration did to a set of documents.
/// </summary>
/// <param name="Plan">The <see cref="MigrationPlan"/> that ran.</param>
/// <param name="Files">What happened to each document, as <see cref="FileMigration">file migrations</see>, ordered by path.</param>
public sealed record MigrationReport(MigrationPlan Plan, IReadOnlyList<FileMigration> Files)
{
    /// <summary>
    /// Gets the outcome of the whole migration: the worst outcome of any document.
    /// </summary>
    public MigrationOutcome Outcome
    {
        get
        {
            if (Files.Any(file => file.Outcome == MigrationOutcome.Failed))
            {
                return MigrationOutcome.Failed;
            }

            return Files.Any(file => file.Outcome == MigrationOutcome.NeedsReview) ? MigrationOutcome.NeedsReview : MigrationOutcome.Migrated;
        }
    }
}

/// <summary>
/// Represents what a migration did to one document.
/// </summary>
/// <param name="Path">The path of the document, relative to the folder migrated.</param>
/// <param name="Original">The text before the migration.</param>
/// <param name="Migrated">The text after the migration; the original text when the migration failed.</param>
/// <param name="Changes">The changes every step made, in step order, as <see cref="StepChange">step changes</see>.</param>
/// <param name="Reviews">What the steps left for an author, as <see cref="MigrationReview">reviews</see>.</param>
/// <param name="Diagnostics">The errors and warnings the target compiler reports in the migrated text.</param>
/// <param name="Failure">Why the document could not be migrated, or <c>null</c> when it was.</param>
public sealed record FileMigration(
    string Path,
    string Original,
    string Migrated,
    IReadOnlyList<StepChange> Changes,
    IReadOnlyList<MigrationReview> Reviews,
    IReadOnlyList<Diagnostic> Diagnostics,
    string? Failure = null)
{
    /// <summary>
    /// Gets a value indicating whether the migration changed the text.
    /// </summary>
    public bool Changed => !string.Equals(Original, Migrated, StringComparison.Ordinal);

    /// <summary>
    /// Gets the outcome for the document.
    /// </summary>
    public MigrationOutcome Outcome
    {
        get
        {
            if (Failure is not null)
            {
                return MigrationOutcome.Failed;
            }

            return Reviews.Count > 0 || Diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error) ? MigrationOutcome.NeedsReview : MigrationOutcome.Migrated;
        }
    }
}

/// <summary>
/// Represents one change, with the step that made it.
/// </summary>
/// <param name="Step">The name of the step.</param>
/// <param name="Change">The <see cref="MigrationChange"/>.</param>
public sealed record StepChange(MigrationName Step, MigrationChange Change);
