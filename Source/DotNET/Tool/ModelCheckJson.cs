// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Mcp;
using Cratis.Screenplay.Validation;

namespace Cratis.Screenplay.Tool;

static class ModelCheckJson
{
    static readonly JsonSerializerOptions _serialization = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    internal static bool Requested(string[] args) => !args.Contains("--print-gate", StringComparer.Ordinal) && args.Zip(args.Skip(1)).Any(pair => pair.First == "--format" && pair.Second == "json");

    internal static int Run(string[] args, TextWriter output)
    {
        using var errors = new StringWriter();
        var options = ModelCheckOptions.Parse(args, errors);
        if (options is null) return Write(new(null, [], null, new CheckRefusal("InvalidInput", errors.ToString().TrimEnd()), 2), output);
        try
        {
            options.IsFile = File.Exists(options.Target);
            if (TargetRefusal(options) is { } reason) return Refuse(options, reason, output);
            var resolution = ModelCheckGate.Resolve(options);
            if (resolution.Failure is { } failure) return Write(new(null, [], null, failure, 2), output);
            return Check(options, output);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return Refuse(options, $"Could not check '{options.Target}': {exception.Message}", output);
        }
    }

    static string? TargetRefusal(ModelCheckOptions options)
    {
        if (!Path.Exists(options.Target)) return $"'{options.Target}' does not exist";
        return options.IsFile && !options.Target.EndsWith(".play", StringComparison.OrdinalIgnoreCase) ? $"'{options.Target}' is not a .play file" : null;
    }

    static int Check(ModelCheckOptions options, TextWriter output)
    {
        var snapshot = McpSnapshot.Compile(options.Target, options.IsFile);
        var diagnostics = snapshot.Compilation.Diagnostics.Concat(snapshot.Completeness(options.Checks)).ToArray();
        ScopedDiagnosticResult? scoped = null;
        if (options.Scope is { } scope)
        {
            if (!ScopedDiagnostics.TryValidate(snapshot, scope, options.Checks, out var selection, out var scopeError)) return Refuse(options, scopeError.Message, output);
            scoped = selection;
            diagnostics = [.. selection.Diagnostics];
        }

        var questions = options.OpenQuestions is null ? null : ModelNoteQuestions.Read(options.Gate!.ModelRoot, options.Scope);
        var exitCode = Math.Max(DiagnosticExit(diagnostics, options.WarnAsError), questions?.GetExitCode(options.OpenQuestions == "fail") ?? 0);
        object? refusal = questions?.FailureKind is { } kind ? new CheckRefusal(kind, questions.Reason!) : null;
        var report = new JsonCheckReport(options.Gate, diagnostics, questions, refusal, exitCode, snapshot.Sources.Count, scoped);
        return Write(report, output);
    }

    static int DiagnosticExit(IEnumerable<Diagnostic> diagnostics, bool warnAsError)
    {
        if (diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)) return 1;
        return warnAsError && diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Warning) ? 1 : 0;
    }

    static int Refuse(ModelCheckOptions options, string reason, TextWriter output) => Write(new(options.Gate, [], null, new CheckRefusal("CouldNotRun", reason), 2), output);

    static int Write(JsonCheckReport report, TextWriter output)
    {
        output.WriteLine(JsonSerializer.Serialize(report, _serialization));
        return report.ExitCode;
    }

    sealed record JsonCheckReport(EffectiveValidationGate? Gate, IEnumerable<Diagnostic> Diagnostics, ModelNoteQuestionsOutcome? OpenQuestions, object? Refusal, int ExitCode, int FilesCompiled = 0, ScopedDiagnosticResult? Scope = null);
    sealed record CheckRefusal(string Kind, string Reason);
}
