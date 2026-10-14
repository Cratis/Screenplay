// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Mcp;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Tool;

sealed record OpenQuestionsRequest(McpSnapshot Snapshot, ModelCheckOptions Options);

static class ModelOpenQuestions
{
    internal const string InvalidMode = "--open-questions requires report or fail and cannot be repeated.";

    internal static bool TrySetMode(ModelCheckOptions options, string value)
    {
        if (options.OpenQuestions is not null) return false;
        if (!new[] { "report", "fail" }.Contains(value, StringComparer.Ordinal)) return false;
        options.OpenQuestions = value;
        return true;
    }

    internal static int WithReport(OpenQuestionsRequest request, TextWriter output, TextWriter error, Func<int> report)
    {
        var notesExit = Run(request, output, error);
        if (notesExit == 2) return 2;
        return Math.Max(report(), notesExit);
    }

    internal static int Run(OpenQuestionsRequest request, TextWriter output, TextWriter error)
    {
        if (request.Options.OpenQuestions is null) return 0;
        try
        {
            var options = request.Options;
            var directory = options.IsFile ? Path.GetDirectoryName(Path.GetFullPath(options.Target))! : Path.GetFullPath(options.Target);
            var rootDirectory = options.Gate?.ModelRoot ?? directory;
            if (!options.UseResolvedNotesRoot) _ = McpTestDocuments.From(request.Snapshot, directory, out rootDirectory);

            return Report(ModelNoteQuestions.Read(rootDirectory, options.Scope), options.OpenQuestions, output, error);
        }
        catch (Exception exception) when (exception is McpFailure or IOException or UnauthorizedAccessException or InvalidSemanticContract or InvalidScreenplayWorkspace or InvalidWorkspaceDocument)
        {
            error.WriteLine($"Could not report model notes: {exception.Message}");
            return 2;
        }
    }

    static int Report(ModelNoteQuestionsOutcome result, string? mode, TextWriter output, TextWriter error)
    {
        if (result.FailureKind is not null)
        {
            error.WriteLine(result.FailureKind == "UnreadableNotes" ? $"UnreadableNotes: {result.Reason}" : $"Could not report model notes: {result.Reason}");
            return 2;
        }

        if (result.IdentityStatus == "absent") output.WriteLine("Identity state absent: .screenplay/identities.json is missing; notes cannot be scoped. Reporting open questions unscoped as orphaned (identity-state-absent).");
        WriteFindings(result.Findings, output, result.OrphanReason);
        output.WriteLine($"Model notes: {result.Findings.Length} open question(s){ScopeSuffix(result.EffectiveScope)}");

        return result.GetExitCode(mode == "fail");
    }

    static string ScopeSuffix(string? scope) => scope is null ? string.Empty : " in scope";

    static void WriteFindings(ImmutableArray<ModelNotesFinding> findings, TextWriter output, string? orphanReason)
    {
        foreach (var finding in findings) output.WriteLine($"{Address(finding, orphanReason)}: {finding.Severity} {finding.Code}: {finding.Message} ({finding.NoteId})");
    }

    static string Address(ModelNotesFinding finding, string? orphanReason)
    {
        if (finding.Address is not null) return string.Join('.', finding.Address.Parts.Where(part => part.Kind != "Application").Select(part => part.Key));
        if (orphanReason is null) return "<orphaned>";
        return $"<orphaned: {orphanReason}>";
    }
}
