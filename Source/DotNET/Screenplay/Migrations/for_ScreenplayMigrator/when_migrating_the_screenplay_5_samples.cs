// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Files;

namespace Cratis.Screenplay.Migrations.for_ScreenplayMigrator;

/// <summary>
/// Migrates every sample as it was at v5.11.0, the last Screenplay 5 release - copied into
/// <c>screenplay-5-samples</c> as <c>.play.txt</c> files - and compiles the result with the current compiler.
/// </summary>
public class when_migrating_the_screenplay_5_samples : Specification
{
    readonly Dictionary<string, MigrationReport> _reports = new(StringComparer.Ordinal);
    readonly List<string> _samplesScreenplay6Refuses = [];
    readonly List<string> _diagnostics = [];
    readonly List<string> _reviews = [];
    readonly List<string> _failures = [];

    // Screenplay 6.x deprecates the lifecycle verbs sends, send and start (PLAY0397); printing a document migrates them, so the migration is not asked to rewrite them.
    static bool IsDeprecatedLifecycleVerb(Diagnostic diagnostic) => diagnostic.Code == DiagnosticCodes.LegacyInlineCodeFence && (diagnostic.Message.Contains("invokes", StringComparison.Ordinal) || diagnostic.Message.Contains("'entering start'", StringComparison.Ordinal));

    // The Screenplay 5 samples set accent, corners and font on the profile; Screenplay 6.x deprecates those (PLAY0929) and the migration is not asked to move them into a theme.
    void Because()
    {
        var migrator = new ScreenplayMigrator();
        var plan = migrator.Plan(5, 6);
        var compiler = new PlayFileCompiler();
        foreach (var sample in Directory.GetDirectories(SamplesFolder()).Order(StringComparer.Ordinal))
        {
            var name = Path.GetFileName(sample);
            var documents = Directory.GetFiles(sample, "*.play.txt", SearchOption.AllDirectories)
                .Select(file => new PlayFileContent(Path.GetRelativePath(sample, file)[..^".txt".Length].Replace('\\', '/'), File.ReadAllText(file)))
                .ToArray();
            if (compiler.CompileFolder(documents).Result.Diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error))
            {
                _samplesScreenplay6Refuses.Add(name);
            }

            var report = migrator.Migrate(documents, plan);
            _reports[name] = report;
            foreach (var file in report.Files)
            {
                _diagnostics.AddRange(file.Diagnostics.Where(diagnostic => diagnostic.Code != DiagnosticCodes.DeprecatedAppearanceMember && !IsDeprecatedLifecycleVerb(diagnostic)).Select(diagnostic => $"{name}/{file.Path}({diagnostic.Location.Line},{diagnostic.Location.Column}): {diagnostic.Code} {diagnostic.Message}"));
                _reviews.AddRange(file.Reviews.Select(review => $"{name}/{file.Path}({review.Location.Line}): {review.Message}"));
                if (file.Failure is not null) _failures.Add($"{name}/{file.Path}: {file.Failure}");
            }
        }
    }

    [Fact] void should_migrate_every_sample() => _reports.Keys.ShouldEqual(["AcmeCommerce", "Administration", "Approvals", "Billing", "Bookings", "Catalog", "Commerce", "Contracts", "Crm", "CustomerPortal", "Hr", "Inventory", "Invoicing", "Library", "Orders", "Organization", "Projects", "Ticketing", "TimeTracking", "Vendors", "WorkOrders"]);

    // Without this the spec would pass for samples that never needed migrating: only Library and Organization use none of the forms Screenplay 6 retired.
    [Fact] void should_start_from_samples_screenplay_6_refuses() => _samplesScreenplay6Refuses.Count.ShouldEqual(_reports.Count - 2);
    [Fact] void should_compile_every_migrated_sample_without_errors_or_warnings() => _diagnostics.ShouldBeEmpty();
    [Fact] void should_need_no_review() => _reviews.ShouldBeEmpty();
    [Fact] void should_refuse_no_document() => _failures.ShouldBeEmpty();
    [Fact] void should_end_with_every_sample_migrated() => _reports.Values.All(report => report.Outcome == MigrationOutcome.Migrated).ShouldBeTrue();
    [Fact] void should_rewrite_every_sample_screenplay_6_refused() => _samplesScreenplay6Refuses.TrueForAll(name => _reports[name].Files.Any(file => file.Changed)).ShouldBeTrue();
    [Fact] void should_leave_the_samples_that_needed_nothing_untouched() => _reports.Where(pair => !_samplesScreenplay6Refuses.Contains(pair.Key)).All(pair => pair.Value.Files.All(file => !file.Changed)).ShouldBeTrue();

    static string SamplesFolder([CallerFilePath] string path = "") => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path)!, "screenplay-5-samples"));
}
