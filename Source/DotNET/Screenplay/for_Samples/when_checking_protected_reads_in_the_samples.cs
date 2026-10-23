// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_Samples;

public class when_checking_protected_reads_in_the_samples : given.the_samples
{
    readonly List<string> _errors = [];
    readonly List<string> _missingPreambles = [];
    readonly List<string> _newErrors = [];
    readonly List<string> _legacySamples = [];
    readonly List<(string Sample, DiagnosticArgument Reason)> _refusals = [];
    int _checked;

    void Because()
    {
        foreach (var sample in _samples.Where(sample => Path.GetFileName(sample) is not ("AcmeCommerce" or "Invoicing")))
        {
            Check(sample);
        }
    }

    void Check(string sample)
    {
        var name = Path.GetFileName(sample);
        var compilation = new PlayFileCompiler().CompileFolder(sample);
        _errors.AddRange(compilation.Result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Select(diagnostic => $"{name}: {diagnostic.Code} {diagnostic.Message}"));
        if (compilation.Result.Value is not { } application) return;
        var (baseline, candidate) = Bind(name, compilation.Sources, application);
        if (name is "Library" or "TimeTracking")
        {
            if (application.CommandReadMode is null) _legacySamples.Add(name);
            _refusals.AddRange(candidate.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.UnprotectableDecisionRead)
                .Select(diagnostic => (name, diagnostic.Arguments["reason"])));
            return;
        }
        _checked++;
        if (application.CommandReadMode != CommandReadMode.Protected) _missingPreambles.Add(name);
        var previous = baseline.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).Select(diagnostic => (diagnostic.Code, diagnostic.Message));
        _newErrors.AddRange(candidate.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Select(diagnostic => (diagnostic.Code, diagnostic.Message)).Except(previous).Select(diagnostic => $"{name}: {diagnostic.Code} {diagnostic.Message}"));
    }

    static (CompilationResult<SemanticCompilation> Baseline, CompilationResult<SemanticCompilation> Candidate) Bind(
        string name, IEnumerable<PlayFileSource> sources, ApplicationSyntax application)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create(name));
        var documents = SemanticDocumentSet.Create([.. sources.Select(source => SemanticSourceDocument.Create(
            catalog.ResolveDocument(source.File.RelativePath.Replace('/', '_')), source.File.RelativePath.Replace('/', '_'), source.File.RelativePath, source.Source))], catalog);
        var binder = new SemanticModelBinder();
        return (binder.Bind(name, application with { CommandReadMode = null }, documents),
            binder.Bind(name, application with { CommandReadMode = CommandReadMode.Protected }, documents));
    }

    [Fact] void should_compile_every_checked_folder() => _errors.ShouldBeEmpty();
    [Fact] void should_check_the_migrated_samples() => _checked.ShouldEqual(16);
    [Fact] void should_opt_in_every_admissible_sample() => _missingPreambles.ShouldBeEmpty();
    [Fact] void should_not_introduce_executable_errors() => _newErrors.ShouldBeEmpty();
    [Fact] void should_leave_the_inadmissible_samples_legacy() => _legacySamples.ShouldEqual(new[] { "Library", "TimeTracking" });
    [Fact] void should_refuse_library_without_a_removal_handler() => _refusals.Any(refusal => refusal.Sample == "Library" && refusal.Reason == DiagnosticArgument.Keyword("no-removal")).ShouldBeTrue();
    [Fact] void should_refuse_time_tracking_composite_reads() => _refusals.Any(refusal => refusal.Sample == "TimeTracking" && refusal.Reason == DiagnosticArgument.Keyword("by-parts")).ShouldBeTrue();
}
