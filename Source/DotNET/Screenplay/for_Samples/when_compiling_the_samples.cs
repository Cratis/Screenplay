// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Strings;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_Samples;

/// <summary>
/// Keeps the samples honest as the language moves: each sample folder compiles as one application with no
/// diagnostics, and holds to the conventions the samples exist to show.
/// </summary>
public partial class when_compiling_the_samples : given.the_samples
{
    readonly List<string> _playFiles = [];
    readonly List<string> _timelineFindings = [];
    readonly List<string> _diagnostics = [];
    readonly List<Diagnostic> _legacyOptionality = [];
    readonly List<string> _slicesWithoutScreens = [];
    readonly List<string> _slicesWithoutSpecifications = [];
    readonly List<string> _missingStrings = [];
    readonly List<string> _unreachable = [];
    int _slices;

    void Because()
    {
        var compiler = new PlayFileCompiler();
        foreach (var sample in _samples)
        {
            var name = Path.GetFileName(sample);
            var compilation = compiler.CompileFolder(sample);
            _timelineFindings.AddRange(compilation.Result.Diagnostics.Where(diagnostic => diagnostic.Code == "PLAY0516" || diagnostic.Code == "PLAY0517")
                .Select(diagnostic => $"{name}: {diagnostic.Code} {TimelineFinding().Match(diagnostic.Message).Value}"));
            _legacyOptionality.AddRange(compilation.Result.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.LegacyOptionalSuffix));
            _playFiles.AddRange(compilation.Sources.Select(source => $"{name}/{source.File.RelativePath}"));
            _diagnostics.AddRange(compilation.Result.Diagnostics
                .Where(diagnostic => diagnostic.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Warning)
                .Select(diagnostic => $"{name}/{diagnostic.Location.Path}({diagnostic.Location.Line},{diagnostic.Location.Column}): {diagnostic.Code} {diagnostic.Message}"));

            if (compilation.Result.Value is { } application)
            {
                CheckSlices(name, application);
            }

            CheckStrings(name, sample, compilation.Sources.Select(source => source.Source));
            CheckRoots(compiler, name, sample, compilation.Sources.Select(source => source.File.RelativePath));
        }
    }

    [Fact]
    void should_report_the_expected_timeline_findings() => _timelineFindings.Order(StringComparer.Ordinal).ShouldEqual(new[]
    {
        "Library: PLAY0516 Slice 'BookCatalog' uses event 'BookBorrowed'",
        "Library: PLAY0516 Slice 'BookCatalog' uses event 'BookReturned'",
        "Invoicing: PLAY0517 'InvoiceManagement', 'Integrations'",
        "Invoicing: PLAY0516 Slice 'InvoiceDashboard' uses event 'InvoiceWrittenOff'",
        "Invoicing: PLAY0516 Slice 'InvoiceAging' uses event 'InvoiceReminderSent'",
        "Invoicing: PLAY0516 Slice 'CollectionsBoard' uses event 'InvoiceReminderSent'",
        "Commerce: PLAY0516 Slice 'ProductList' uses event 'ProductRegistered'",
        "Commerce: PLAY0516 Slice 'ProductList' uses event 'ProductPriceChanged'",
        "Commerce: PLAY0516 Slice 'MyOrders' uses event 'OrderPlaced'",
        "Commerce: PLAY0516 Slice 'MyOrders' uses event 'OrderPaid'",
        "Commerce: PLAY0516 Slice 'ShipmentQueue' uses event 'ShipmentRequested'",
        "Commerce: PLAY0517 'Shipping', 'Tracking'",
        "Commerce: PLAY0517 'Ordering', 'Fulfillment'",
        "TimeTracking: PLAY0516 Slice 'PayrollRuns' uses event 'PayrollRunAcknowledged'",
        "TimeTracking: PLAY0516 Slice 'RemindConsultants' reads read model 'DraftTimesheet'",
        "TimeTracking: PLAY0516 Slice 'MyTimesheets' uses event 'TimesheetStarted'",
        "TimeTracking: PLAY0516 Slice 'MyTimesheets' uses event 'TimesheetSubmitted'",
        "TimeTracking: PLAY0516 Slice 'MyTimesheets' uses event 'TimesheetApproved'",
        "TimeTracking: PLAY0516 Slice 'MyTimesheets' uses event 'TimesheetRejected'",
        "TimeTracking: PLAY0516 Slice 'QueueApprovedTimesheets' uses event 'TimesheetApproved'",
        "TimeTracking: PLAY0516 Slice 'BookTimeOff' uses event 'AbsenceReported'"
    }.Order(StringComparer.Ordinal));

    [Fact] void should_find_the_samples() => _samples.ShouldNotBeEmpty();
    [Fact] void should_find_play_files_in_every_sample() => _samples.Select(Path.GetFileName).Except(_playFiles.Select(file => file.Split('/')[0])).ShouldBeEmpty();
    [Fact] void should_find_slices() => _slices.ShouldBeGreaterThan(0);
    [Fact] void should_compile_without_errors_or_warnings() => _diagnostics.ShouldBeEmpty();
    [Fact] void should_use_only_canonical_optionality() => _legacyOptionality.ShouldBeEmpty();
    [Fact] void should_give_every_state_change_and_state_view_slice_a_screen() => _slicesWithoutScreens.ShouldBeEmpty();
    [Fact] void should_give_every_slice_a_specification() => _slicesWithoutSpecifications.ShouldBeEmpty();
    [Fact] void should_define_every_referenced_string_in_every_locale() => _missingStrings.ShouldBeEmpty();
    [Fact] void should_reach_every_file_from_the_root_composite() => _unreachable.ShouldBeEmpty();

    void CheckSlices(string sample, ApplicationSyntax application)
    {
        foreach (var module in application.Modules)
        {
            foreach (var slice in module.Features.SelectMany(SlicesOf))
            {
                _slices++;
                var name = $"{sample}: {module.Name} {slice.Type} {slice.Name}";
                if (slice.Type is SliceType.StateChange or SliceType.StateView && !slice.Screens.Any())
                {
                    _slicesWithoutScreens.Add(name);
                }

                if (!slice.Specifications.Any())
                {
                    _slicesWithoutSpecifications.Add(name);
                }
            }
        }
    }

    static IEnumerable<SliceSyntax> SlicesOf(FeatureSyntax feature) =>
        feature.Slices.Concat(feature.Features.SelectMany(SlicesOf));

    // A sample whose root file imports others is composed from that root, so every file in the folder has to be
    // reachable from it - a file nothing imports is a file the application, compiled from its root, leaves out.
    void CheckRoots(PlayFileCompiler compiler, string sample, string folder, IEnumerable<string> folderFiles)
    {
        var roots = Directory.GetFiles(folder, "*.play").Where(file => FileImport().IsMatch(File.ReadAllText(file))).ToList();
        if (roots.Count == 0)
        {
            return;
        }

        var reached = roots.SelectMany(root => compiler.CompileApplication(root).Sources.Select(source => source.File.RelativePath)).ToHashSet(StringComparer.Ordinal);
        _unreachable.AddRange(folderFiles.Where(file => !reached.Contains(file)).Order(StringComparer.Ordinal).Select(file => $"{sample}/{file}"));
    }

    void CheckStrings(string sample, string folder, IEnumerable<string> sources)
    {
        var referenced = sources
            .SelectMany(source => StringReference().Matches(source))
            .Select(match => match.Groups[1].Value.TrimEnd('.'))
            .ToHashSet(StringComparer.Ordinal);
        if (referenced.Count == 0)
        {
            return;
        }

        var stringsFiles = Directory.GetFiles(folder, "*.strings", SearchOption.AllDirectories);
        if (stringsFiles.Length == 0)
        {
            _missingStrings.Add($"{sample}: references $strings keys but has no .strings file");
            return;
        }

        foreach (var file in stringsFiles)
        {
            var defined = StringsFile.Parse(File.ReadAllText(file)).Entries.Select(entry => entry.Key).ToHashSet(StringComparer.Ordinal);
            _missingStrings.AddRange(referenced.Except(defined).Order(StringComparer.Ordinal).Select(key => $"{sample}/{Path.GetFileName(file)}: {key}"));
        }
    }

    [GeneratedRegex(@"Slice '[^']+' (?:uses event|reads read model) '[^']+'|'Ordering', 'Fulfillment'|'Shipping', 'Tracking'|'InvoiceManagement', 'Integrations'|'Runs', 'Handover'|'Recording', 'Reporting'", RegexOptions.None, 1000)]
    private static partial Regex TimelineFinding();

    [GeneratedRegex(@"^\s*import\s+""", RegexOptions.Multiline, 1000)]
    private static partial Regex FileImport();

    [GeneratedRegex(@"\$strings\.([A-Za-z_][\w.]*)", RegexOptions.None, 1000)]
    private static partial Regex StringReference();
}
