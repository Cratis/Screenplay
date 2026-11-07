// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Printing;

namespace Cratis.Screenplay.Migrations;

/// <summary>
/// Runs migrations: parses each document as the parser reads it - retired forms included - runs every step of the plan
/// in order, prints the rewritten tree into the text and compiles the result with the current compiler.
/// </summary>
/// <param name="steps">The steps to chain.</param>
public sealed class ScreenplayMigrator(IEnumerable<IScreenplayMigration> steps)
{
    readonly ScreenplayCompiler _compiler = new();
    readonly ScreenplayPrinter _printer = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ScreenplayMigrator"/> class with the steps found by convention.
    /// </summary>
    public ScreenplayMigrator()
        : this(ScreenplayMigrations.Discover())
    {
    }

    /// <summary>
    /// Gets the steps the migrator chains.
    /// </summary>
    public IReadOnlyList<IScreenplayMigration> Steps { get; } = [.. steps];

    /// <summary>
    /// Chains the steps that move a document from one version to another.
    /// </summary>
    /// <param name="from">The version the documents are written in.</param>
    /// <param name="to">The version to end at.</param>
    /// <returns>The <see cref="MigrationPlan"/>.</returns>
    /// <exception cref="InvalidMigrationPlan">The steps cannot move a document from <paramref name="from"/> to <paramref name="to"/>.</exception>
    public MigrationPlan Plan(ScreenplayVersion from, ScreenplayVersion to) => ScreenplayMigrations.Plan(Steps, from, to);

    /// <summary>
    /// Migrates the <c>.play</c> files of a folder - or one file, with the files of its folder as its context.
    /// </summary>
    /// <param name="path">A <c>.play</c> file or a folder.</param>
    /// <param name="plan">The <see cref="MigrationPlan"/> to run.</param>
    /// <returns>The <see cref="MigrationReport"/>; nothing is written.</returns>
    public MigrationReport MigratePath(string path, MigrationPlan plan)
    {
        var isFile = File.Exists(path);
        var root = isFile ? System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(path))! : path;
        var playFiles = new PlayFiles();
        var documents = playFiles.FindIn(root).Select(file => new PlayFileContent(file.RelativePath, playFiles.ReadContent(file))).ToArray();
        var only = isFile ? PlayGlob.Normalize(System.IO.Path.GetRelativePath(root, System.IO.Path.GetFullPath(path))) : null;
        return Migrate(documents, plan, only is null ? null : relative => string.Equals(relative, only, StringComparison.Ordinal));
    }

    /// <summary>
    /// Migrates a set of documents that make up one application.
    /// </summary>
    /// <param name="documents">The documents, with paths relative to the folder they make up.</param>
    /// <param name="plan">The <see cref="MigrationPlan"/> to run.</param>
    /// <param name="select">Which documents to migrate, by relative path; every document when <c>null</c>. The others are
    /// compiled as they are.</param>
    /// <returns>The <see cref="MigrationReport"/> for the migrated documents.</returns>
    public MigrationReport Migrate(IEnumerable<PlayFileContent> documents, MigrationPlan plan, Func<string, bool>? select = null)
    {
        var contents = documents.Select(document => document with { RelativePath = PlayGlob.Normalize(document.RelativePath) }).ToArray();
        var placements = Placements(contents);
        var files = contents
            .Where(document => select?.Invoke(document.RelativePath) ?? true)
            .OrderBy(document => document.RelativePath, StringComparer.Ordinal)
            .Select(document => MigrateDocument(document.RelativePath, document.Content, placements.GetValueOrDefault(document.RelativePath, PlayPlacement.Document), plan))
            .ToArray();

        // Compile the documents as they will be: migrated where they were migrated, as they are elsewhere.
        var migrated = files.ToDictionary(file => file.Path, file => file.Migrated, StringComparer.Ordinal);
        var compilation = new PlayFileCompiler().CompileFolder(contents.Select(document => migrated.TryGetValue(document.RelativePath, out var text) ? document with { Content = text } : document));
        var diagnostics = compilation.Result.Diagnostics
            .Where(diagnostic => diagnostic.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Warning)
            .ToLookup(diagnostic => PlayGlob.Normalize(diagnostic.Location.Path ?? string.Empty), StringComparer.Ordinal);
        return new(plan, [.. files.Select(file => file.Failure is null ? file with { Diagnostics = [.. diagnostics[file.Path]] } : file)]);
    }

    /// <summary>
    /// Migrates one document, without compiling the result.
    /// </summary>
    /// <param name="path">The path of the document, as locations in it report.</param>
    /// <param name="source">The source text.</param>
    /// <param name="placement">Where the document's content sits in the application.</param>
    /// <param name="plan">The <see cref="MigrationPlan"/> to run.</param>
    /// <returns>The <see cref="FileMigration"/>, without diagnostics.</returns>
    public FileMigration MigrateDocument(string path, string source, PlayPlacement placement, MigrationPlan plan)
    {
        var text = source;
        var changes = new List<StepChange>();
        var reviews = new List<MigrationReview>();
        foreach (var step in plan.Steps)
        {
            var parsed = _compiler.Parse(text, path, placement);
            if (parsed.Value is not { } syntax)
            {
                return new(path, source, source, changes, reviews, [], $"The {step.From} parser could not read the document");
            }

            try
            {
                var result = step.Apply(new(path, text, placement, syntax));
                text = MigrationText.Apply(text, result.Changes.SelectMany(change => change.Edits), _printer);
                changes.AddRange(result.Changes.Select(change => new StepChange(step.Name, change)));
                reviews.AddRange(result.Reviews);
            }
            catch (InvalidMigrationEdit exception)
            {
                return new(path, source, source, changes, reviews, [], $"The step '{step.Name}' could not rewrite the document: {exception.Message}");
            }
        }

        return new(path, source, text, changes, reviews, []);
    }

    // Where each document sits decides how it reads - a file an import puts in a module holds module content - so resolve
    // placements the way the folder compiler does, from the documents as they are.
    static Dictionary<string, PlayPlacement> Placements(PlayFileContent[] contents)
    {
        var placements = new Dictionary<string, PlayPlacement>(StringComparer.Ordinal);
        if (contents.Length == 0)
        {
            return placements;
        }

        var folder = new MemoryPlayDocuments(contents);
        var (placed, _) = PlayImports.Resolve([.. folder.Source.FilesBeneath(string.Empty)], folder.Source);
        foreach (var document in placed.Where(document => document.IsPlacementResolved))
        {
            placements.TryAdd(PlayGlob.Normalize(document.Path), document.Placement);
        }

        return placements;
    }
}
