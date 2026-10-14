// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Completeness;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Mcp;

namespace Cratis.Screenplay.Tool;

static class ModelCheck
{
    internal static int Run(string[] args, TextWriter output, TextWriter error, bool useColors = false)
    {
        if (ModelCheckJson.Requested(args)) return ModelCheckJson.Run(args, output);
        var options = ModelCheckOptions.Parse(args, error);
        if (options is null) return 2;
        options.IsFile = File.Exists(options.Target);
        if (!options.PrintGate && !ValidateTarget(options, error)) return 2;
        try
        {
            return RunResolved(options, output, error, useColors);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            error.WriteLine($"Could not check '{options.Target}': {exception.Message}");
            return 2;
        }
    }

    static int RunResolved(ModelCheckOptions options, TextWriter output, TextWriter error, bool useColors)
    {
        var resolution = ModelCheckGate.Resolve(options);
        if (resolution.Failure is { } failure)
        {
            error.WriteLine($"{failure.Path}({failure.Line},{failure.Column}): {failure.Kind}: {failure.Reason}");
            return 2;
        }

        if (!options.PrintGate) return Check(options, output, error, useColors && !options.NoColor);
        ModelCheckGate.Print(resolution.Gate!, output);
        return 0;
    }

    static bool ValidateTarget(ModelCheckOptions options, TextWriter error)
    {
        if (!Exists(options))
        {
            error.WriteLine($"'{options.Target}' does not exist");
            return false;
        }

        if (!IsPlayInput(options))
        {
            error.WriteLine($"'{options.Target}' is not a .play file");
            return false;
        }

        return true;
    }

    static bool Exists(ModelCheckOptions options) => options.IsFile || Directory.Exists(options.Target);

    static bool IsPlayInput(ModelCheckOptions options) => !options.IsFile || options.Target.EndsWith(".play", StringComparison.OrdinalIgnoreCase);

    static int Check(ModelCheckOptions options, TextWriter output, TextWriter error, bool useColors)
    {
        var snapshot = McpSnapshot.Compile(options.Target, options.IsFile);
        var compilation = snapshot.Compilation;
        var additional = snapshot.Completeness(options.Checks);
        var wholeDiagnostics = compilation.Diagnostics.Concat(additional).ToArray();
        IEnumerable<Diagnostic> diagnostics = wholeDiagnostics;
        if (options.Scope is { } scope)
        {
            if (!ScopedDiagnostics.TryValidate(snapshot, scope, options.Checks, out var selection, out var scopeError))
            {
                error.WriteLine(scopeError.Message);
                return 2;
            }

            diagnostics = selection.Diagnostics;
            WriteScope(selection, scope, output);
            WriteWholeApplication(new(snapshot, diagnostics, options, useColors), selection, wholeDiagnostics.Length, output);
        }

        ReportSkippedChecks(snapshot, options.Checks, output);
        var report = new ModelCheckReport(snapshot, diagnostics, options, useColors);
        return ModelOpenQuestions.WithReport(new(snapshot, options), output, error, () => Report(report, output));
    }

    static void WriteScope(ScopedDiagnosticResult selection, string scope, TextWriter output)
    {
        output.WriteLine($"Scope {scope}: {selection.DeclarationCount} declaration(s), {selection.DependentDeclarationCount} direct dependent declaration(s), {selection.Diagnostics.Length} diagnostic(s)");
        output.WriteLine($"Affected scopes: {(selection.AffectedScopes.Length == 0 ? "none" : string.Join(", ", selection.AffectedScopes.Select(affected => affected.Length == 0 ? "<application>" : affected)))}");
        output.WriteLine($"Unresolved event consumers (cannot be attributed to a scope): {selection.UnresolvedEventConsumers.ReferenceCount} reference(s) in {(selection.UnresolvedEventConsumers.Scopes.Length == 0 ? "none" : string.Join(", ", selection.UnresolvedEventConsumers.Scopes.Select(consumerScope => consumerScope.Length == 0 ? "<application>" : consumerScope)))}");
        output.WriteLine($"Possibly affected: {selection.PossiblyAffectedReferenceCount} other unresolved reference(s) outside the reported declarations");
        output.WriteLine($"Dependency coverage: {selection.DependencyCoverage}");
    }

    static void WriteWholeApplication(ModelCheckReport report, ScopedDiagnosticResult selection, int wholeCount, TextWriter output)
    {
        var wholeErrors = selection.WholeApplicationErrorCount;
        var wholeWarnings = selection.WholeApplicationWarningCount;
        var wholeSummary = $"Whole application: {wholeErrors} error(s), {wholeWarnings} warning(s) ({wholeCount - report.Diagnostics.Count()} outside the reported set)";
        var wholeFailed = IsFailure(report.Snapshot.Compilation.Success ? 0 : 1, wholeWarnings, report.Options.WarnAsError);
        output.WriteLine(report.UseColors && wholeFailed ? $"\e[31m{wholeSummary}\e[0m" : wholeSummary);
    }

    static void ReportSkippedChecks(McpSnapshot snapshot, CompletenessChecks checks, TextWriter output)
    {
        var errorCount = snapshot.Compilation.Diagnostics.Count(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        if (checks.Selected.Count == 0) return;
        if (errorCount > 0) output.WriteLine($"completeness checks skipped: the model has {errorCount} error(s)");
    }

    static int Report(ModelCheckReport report, TextWriter output)
    {
        if (report.Snapshot.Sources.Count == 0)
        {
            output.WriteLine($"No .play files found beneath {report.Options.Target}");
            return 0;
        }

        WriteDiagnostics(report, output);
        var errors = report.Diagnostics.Count(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        var warnings = report.Diagnostics.Count(diagnostic => diagnostic.Severity == DiagnosticSeverity.Warning);
        if (errors + warnings > 0) output.WriteLine();
        var failed = IsFailure(errors, warnings, report.Options.WarnAsError);
        WriteSummary(new(errors, warnings, failed), report, output);
        return failed ? 1 : 0;
    }

    static bool IsFailure(int errors, int warnings, bool warnAsError)
    {
        if (errors > 0) return true;
        if (!warnAsError) return false;
        return warnings > 0;
    }

    static void WriteDiagnostics(ModelCheckReport report, TextWriter output)
    {
        var sources = report.Snapshot.Sources;
        var fallbackFile = sources.Keys.First();
        var formatter = new DiagnosticFormatter();
        foreach (var diagnostic in report.Diagnostics.OrderBy(diagnostic => diagnostic.Location.Path ?? fallbackFile, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.Location.Line).ThenBy(diagnostic => diagnostic.Location.Column))
        {
            var file = diagnostic.Location.Path ?? fallbackFile;
            output.WriteLine(formatter.Format(file, diagnostic, sources.TryGetValue(file, out var source) ? source : string.Empty, report.UseColors));
        }
    }

    static void WriteSummary(ModelCheckCounts counts, ModelCheckReport report, TextWriter output)
    {
        var summary = $"{report.Snapshot.Sources.Count} file(s) compiled - {counts.Errors} error(s), {counts.Warnings} warning(s){(report.Options.Scope is null ? string.Empty : " in scope")}";
        if (!report.UseColors)
        {
            output.WriteLine(summary);
            return;
        }

        var color = (counts.Failed, counts.Warnings > 0) switch
        {
            (true, _) => "\e[31m",
            (_, true) => "\e[33m",
            _ => "\e[32m"
        };
        output.WriteLine($"{color}{summary}\e[0m");
    }
}

sealed record ModelCheckReport(McpSnapshot Snapshot, IEnumerable<Diagnostic> Diagnostics, ModelCheckOptions Options, bool UseColors);
sealed record ModelCheckCounts(int Errors, int Warnings, bool Failed);
