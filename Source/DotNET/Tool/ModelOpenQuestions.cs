// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Mcp;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Tool;

sealed record OpenQuestionsRequest(McpSnapshot Snapshot, string Target, bool IsFile, string? Scope, string? Mode);

static class ModelOpenQuestions
{
    internal static int Run(OpenQuestionsRequest request, TextWriter output, TextWriter error)
    {
        if (request.Mode is null) return 0;
        try
        {
            var directory = request.IsFile ? Path.GetDirectoryName(Path.GetFullPath(request.Target))! : Path.GetFullPath(request.Target);
            var documents = McpTestDocuments.From(request.Snapshot, directory, out var rootDirectory);
            var root = new McpRoot(rootDirectory);
            var persisted = new McpManagedFiles(root).Read(McpState.FileName);
            var state = persisted is null ? null : McpState.Deserialize(persisted);
            var name = state?.ApplicationName ?? new DirectoryInfo(rootDirectory).Name;
            var identity = state?.Catalog.Application ?? ApplicationIdentity.Create(name);
            var notes = ModelNotesStore.Read(rootDirectory, identity);
            if (notes.Status == "unreadable")
            {
                error.WriteLine($"UnreadableNotes: {notes.Reason}");
                return 2;
            }

            var workspace = state?.Open(root) ?? (documents.IsEmpty ? ScreenplayWorkspace.CreateEmpty(identity, name) : ScreenplayWorkspace.Create(identity, name, documents, SemanticIdentityCatalog.Empty(identity)));
            var findings = ModelNotesAnchors.Findings(notes.Document!, workspace.IdentityCatalog, request.Scope);
            foreach (var finding in findings)
            {
                var address = finding.Address is null ? "<orphaned>" : string.Join('.', finding.Address.Parts.Where(part => part.Kind != "Application").Select(part => part.Key));
                output.WriteLine($"{address}: {finding.Severity} {finding.Code}: {finding.Message} ({finding.NoteId})");
            }

            output.WriteLine($"Model notes: {findings.Length} open question(s){(request.Scope is null ? string.Empty : " in scope")}");
            return request.Mode == "fail" && !findings.IsEmpty ? 1 : 0;
        }
        catch (Exception exception) when (exception is McpFailure or IOException or UnauthorizedAccessException or InvalidSemanticContract or InvalidScreenplayWorkspace or InvalidWorkspaceDocument)
        {
            error.WriteLine($"Could not report model notes: {exception.Message}");
            return 2;
        }
    }
}
