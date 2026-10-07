// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Mcp;

namespace Cratis.Screenplay.Tool;

static class ModelCheck
{
    internal static int Run(string[] args, TextWriter output, TextWriter error, bool useColors = false)
    {
        string? target = null;
        string? scope = null;
        for (var index = 0; index < args.Length; index++)
        {
            var argument = args[index];
            if (argument == "--scope")
            {
                if (scope is not null || index + 1 == args.Length || args[index + 1].StartsWith('-') || string.IsNullOrWhiteSpace(args[index + 1]))
                {
                    error.WriteLine("--scope requires one module, feature or slice address.");
                    return 2;
                }

                scope = args[++index];
            }
            else if (argument is not "--no-color" and not "--warnaserror")
            {
                if (argument.StartsWith('-') || target is not null)
                {
                    error.WriteLine($"Unexpected argument '{argument}'.");
                    return 2;
                }

                target = argument;
            }
        }

        target ??= Directory.GetCurrentDirectory();
        var isFile = File.Exists(target);
        if (!isFile && !Directory.Exists(target))
        {
            error.WriteLine($"'{target}' does not exist");
            return 2;
        }

        if (isFile && !target.EndsWith(".play", StringComparison.OrdinalIgnoreCase))
        {
            error.WriteLine($"'{target}' is not a .play file");
            return 2;
        }

        try
        {
            return Check(target, isFile, scope, args.Contains("--warnaserror"), output, error, useColors && !args.Contains("--no-color"));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            error.WriteLine($"Could not check '{target}': {exception.Message}");
            return 2;
        }
    }

    static int Check(string target, bool isFile, string? scope, bool warnAsError, TextWriter output, TextWriter error, bool useColors)
    {
        var snapshot = McpSnapshot.Compile(target, isFile);
        var sources = snapshot.Sources;
        var compilation = snapshot.Compilation;
        var diagnostics = compilation.Diagnostics;
        if (scope is not null)
        {
            var selection = ScopedDiagnostics.Select(snapshot, scope, out var scopeError);
            if (selection is null)
            {
                error.WriteLine(scopeError);
                return 2;
            }

            diagnostics = selection.Diagnostics;
            output.WriteLine($"Scope {scope}: {selection.DeclarationCount} declaration(s), {selection.DependentDeclarationCount} direct dependent declaration(s), {selection.Diagnostics.Length} diagnostic(s)");
            output.WriteLine($"Affected scopes: {(selection.AffectedScopes.Length == 0 ? "none" : string.Join(", ", selection.AffectedScopes.Select(affected => affected.Length == 0 ? "<application>" : affected)))}");
            output.WriteLine($"Unresolved event consumers (cannot be attributed to a scope): {selection.UnresolvedEventConsumers.ReferenceCount} reference(s) in {(selection.UnresolvedEventConsumers.Scopes.Length == 0 ? "none" : string.Join(", ", selection.UnresolvedEventConsumers.Scopes.Select(consumerScope => consumerScope.Length == 0 ? "<application>" : consumerScope)))}");
            output.WriteLine($"Possibly affected: {selection.PossiblyAffectedReferenceCount} other unresolved reference(s) outside the reported declarations");
            output.WriteLine($"Dependency coverage: {selection.DependencyCoverage}");
            var wholeErrors = compilation.Diagnostics.Count(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
            var wholeWarnings = compilation.Diagnostics.Count(diagnostic => diagnostic.Severity == DiagnosticSeverity.Warning);
            var wholeSummary = $"Whole application: {wholeErrors} error(s), {wholeWarnings} warning(s) ({compilation.Diagnostics.Count() - diagnostics.Count()} outside the reported set)";
            var wholeFailed = !compilation.Success || (warnAsError && wholeWarnings > 0);
            output.WriteLine(useColors && wholeFailed ? $"\e[31m{wholeSummary}\e[0m" : wholeSummary);
        }

        if (sources.Count == 0)
        {
            output.WriteLine($"No .play files found beneath {target}");
            return 0;
        }

        var fallbackFile = sources.Keys.First();
        var formatter = new DiagnosticFormatter();
        var errors = 0;
        var warnings = 0;
        foreach (var diagnostic in diagnostics
            .OrderBy(diagnostic => diagnostic.Location.Path ?? fallbackFile, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.Location.Line)
            .ThenBy(diagnostic => diagnostic.Location.Column))
        {
            switch (diagnostic.Severity)
            {
                case DiagnosticSeverity.Error:
                    errors++;
                    break;
                case DiagnosticSeverity.Warning:
                    warnings++;
                    break;
            }

            var file = diagnostic.Location.Path ?? fallbackFile;
            output.WriteLine(formatter.Format(file, diagnostic, sources.TryGetValue(file, out var source) ? source : string.Empty, useColors));
        }

        if (errors + warnings > 0)
        {
            output.WriteLine();
        }
        var failed = errors > 0 || (warnAsError && warnings > 0);
        var summary = $"{sources.Count} file(s) compiled - {errors} error(s), {warnings} warning(s){(scope is null ? string.Empty : " in scope")}";
        if (useColors)
        {
            var color = (failed, warnings > 0) switch
            {
                (true, _) => "\e[31m",
                (_, true) => "\e[33m",
                _ => "\e[32m"
            };
            output.WriteLine($"{color}{summary}\e[0m");
        }
        else
        {
            output.WriteLine(summary);
        }

        return failed ? 1 : 0;
    }
}
