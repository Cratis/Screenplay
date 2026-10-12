// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
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
            return Report(request, output, error);
        }
        catch (Exception exception) when (IsReadFailure(exception))
        {
            error.WriteLine($"Could not report model notes: {exception.Message}");
            return 2;
        }
    }

    static bool IsReadFailure(Exception exception) => exception is McpFailure or IOException or UnauthorizedAccessException or InvalidSemanticContract or InvalidScreenplayWorkspace or InvalidWorkspaceDocument;

    static int Report(OpenQuestionsRequest request, TextWriter output, TextWriter error)
    {
        var input = ModelNotesInput.Read(request);
        var notes = input.Notes;
        if (notes.Status == "unreadable")
        {
            error.WriteLine($"UnreadableNotes: {notes.Reason}");
            return 2;
        }

        input.ReportIdentityStatus(output);
        var findings = input.Findings(notes.Document!, request.Options.Scope);
        WriteFindings(findings, output, input.OrphanReason);
        output.WriteLine($"Model notes: {findings.Length} open question(s){ScopeSuffix(input.EffectiveScope(request.Options.Scope))}");
        return QuestionsExit(request.Options.OpenQuestions, findings);
    }

    static int QuestionsExit(string? mode, ImmutableArray<ModelNotesFinding> findings)
    {
        if (mode != "fail") return 0;
        return findings.IsEmpty ? 0 : 1;
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

sealed record ModelNotesInput(McpRoot Root, ImmutableArray<WorkspaceDocument> Documents, McpState? State)
{
    internal string Name => State?.ApplicationName ?? new DirectoryInfo(Root.DirectoryPath).Name;
    internal ApplicationIdentity Identity => State?.Catalog.Application ?? ApplicationIdentity.Create(Name);
    internal ModelNotesRead Notes => State is null ? UnscopedNotes() : ModelNotesStore.Read(Root.DirectoryPath, Identity);
    internal string? OrphanReason => State is null ? "identity-state-absent" : null;

    internal static ModelNotesInput Read(OpenQuestionsRequest request)
    {
        var options = request.Options;
        var directory = options.IsFile ? Path.GetDirectoryName(Path.GetFullPath(options.Target))! : Path.GetFullPath(options.Target);
        var documents = McpTestDocuments.From(request.Snapshot, directory, out var rootDirectory);
        var root = new McpRoot(rootDirectory);
        var persisted = new McpManagedFiles(root).Read(McpState.FileName);
        return new(root, documents, persisted is null ? null : McpState.Deserialize(persisted));
    }

    internal string? EffectiveScope(string? scope) => State is null ? null : scope;

    internal ImmutableArray<ModelNotesFinding> Findings(ModelNotesDocument notes, string? scope)
    {
        if (State is not null) return ModelNotesAnchors.Findings(notes, State.Open(Root).IdentityCatalog, scope);
        return [.. notes.Notes.Where(note => note.Kind == "question" && note.Resolution is null).OrderBy(note => note.Id, StringComparer.Ordinal)
            .Select(note => new ModelNotesFinding(note.Id, "info", "model-note.open-question", note.Text, null))];
    }

    internal void ReportIdentityStatus(TextWriter output)
    {
        if (State is null) output.WriteLine("Identity state absent: .screenplay/identities.json is missing; notes cannot be scoped. Reporting open questions unscoped as orphaned (identity-state-absent).");
    }

    ModelNotesRead UnscopedNotes()
    {
        var bytes = new McpManagedFiles(Root).Read(ModelNotesStore.FileName, ModelNotesStore.MaximumBytes);
        if (bytes is null) return ModelNotesStore.ReadBytes(null, Identity);
        try
        {
            using var json = JsonDocument.Parse(bytes);
            var application = ApplicationIdentity.Parse(json.RootElement.GetProperty("applicationIdentity").GetString()!);
            return ModelNotesStore.ReadBytes(bytes, application);
        }
        catch (Exception exception) when (exception is JsonException or KeyNotFoundException or InvalidOperationException or InvalidSemanticContract)
        {
            return new("unreadable", null, null, exception.Message);
        }
    }
}
