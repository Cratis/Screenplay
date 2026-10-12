// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

internal sealed partial class McpWorkspaces
{
    internal object ListNotes(JsonElement arguments)
    {
        var workspace = Current();
        var notes = ReadNotes(workspace);
        RefuseUnreadableNotes(notes);
        CheckContinuation(arguments, "expectedNotesRevision", notes.Revision!);
        var state = McpJson.OptionalString(arguments, "state") ?? "open";
        var kind = McpJson.OptionalString(arguments, "kind");
        var reference = McpJson.OptionalString(arguments, "ref");
        var scope = McpJson.OptionalString(arguments, "scope");
        var anchor = arguments.TryGetProperty("anchor", out var target) ? McpNotesTargets.Read(target, workspace, allowOrphan: true) : null;
        var filter = new McpNotesFilter(state, kind, anchor, reference, scope);
        var items = ModelNotesAnchors.Items(notes.Document!, workspace.IdentityCatalog).Where(item => filter.Matches(item, workspace.IdentityCatalog));
        var view = McpJson.OptionalString(arguments, "view") ?? "items";
        object page = view == "findings"
            ? McpPaging.Page(ModelNotesAnchors.Findings(notes.Document! with { Notes = [.. items.Select(item => item.Note)] }, workspace.IdentityCatalog, scope), arguments, notes.Revision!)
            : McpPaging.Page(items, arguments, notes.Revision!);
        return McpJson.ToolResult(new { notesRevision = notes.Revision, view, page });
    }

    internal object ProposeNotes(JsonElement arguments)
    {
        var workspace = CheckedCurrent(arguments);
        if (McpJson.RequiredString(arguments, "expectedCatalogRevision") != workspace.IdentityCatalog.Revision.ToString()) throw new McpFailure("Catalog changed.") { FailureKind = "StaleRevision" };
        var before = ReadNotes(workspace);
        RefuseUnreadableNotes(before);
        var bytes = ReadNotesBytes();
        if (McpJson.RequiredString(arguments, "expectedNotesRevision") != before.Revision) throw new McpFailure("Notes changed; read them again.") { FailureKind = "NotesRevisionDrift" };
        var substitutions = new List<object>();
        var after = McpNotesOperations.Apply(before.Document!, workspace, arguments.GetProperty("operations"), substitutions);
        var canonical = ModelNotesStore.Serialize(after);
        var plan = new McpNotesPlan(bytes, canonical, before.Revision!, ModelNotesStore.Revision(canonical));
        var proposal = new McpNotesProposal(workspace, plan);
        BeforeStore?.Invoke();
        RefusePendingWorkspace();
        VerifyNotesPlan(proposal);
        if (_proposals.Count >= 16) throw new McpFailure("The session already holds 16 proposals.") { FailureKind = "LimitExceeded" };
        var id = Guid.NewGuid().ToString("N");
        var state = new McpStatePlan(_stateBytes, McpState.Serialize(workspace));
        var result = McpJson.ToolResult(new { success = true, proposalId = id, notesRevision = plan.BeforeRevision, afterNotesRevision = plan.AfterRevision, addedNotes = after.Notes.Where(note => !before.Document!.Notes.Any(original => original.Id == note.Id)).Select(note => note.Id), substitutions, changeCount = 0, stateChange = state.Describe(), review = "Read read-proposal view=notes before apply." });
        _proposals.Add(id, proposal);
        _statePlans.Add(proposal, state);
        return result;
    }

    static void RefuseUnreadableNotes(ModelNotesRead notes)
    {
        if (notes.Status == "unreadable") throw new McpFailure($"UnreadableNotes: {notes.Reason}") { FailureKind = "UnreadableNotes" };
    }

    object NotesView(IMcpProposal proposal, JsonElement arguments)
    {
        if (proposal is not McpNotesProposal notes) throw new McpFailure("This proposal does not write model notes.", -32602);
        var before = ModelNotesStore.ReadBytes(notes.Notes.Before, proposal.Before.IdentityCatalog.Application);
        var after = ModelNotesStore.ReadBytes(notes.Notes.After, proposal.Workspace.IdentityCatalog.Application);
        CheckContinuation(arguments, "expectedNotesRevision", notes.Notes.AfterRevision);
        var ids = before.Document!.Notes.Select(note => note.Id).Union(after.Document!.Notes.Select(note => note.Id), StringComparer.Ordinal).Order(StringComparer.Ordinal);
        return new
        {
            beforeNotesRevision = notes.Notes.BeforeRevision,
            afterNotesRevision = notes.Notes.AfterRevision,
            page = McpPaging.Page(ids.Select(id => new { id, before = before.Document.Notes.SingleOrDefault(note => note.Id == id), after = after.Document.Notes.SingleOrDefault(note => note.Id == id) }), arguments, notes.Notes.AfterRevision)
        };
    }

    Dictionary<string, object?> WithNotes(object value, ScreenplayWorkspace workspace) => WithNotes(value, workspace.IdentityCatalog.Application);

    Dictionary<string, object?> WithNotes(object value, ApplicationIdentity application)
    {
        var notes = ReadNotes(application);
        var properties = JsonSerializer.SerializeToElement(value, McpJson.Options).EnumerateObject().ToDictionary(property => property.Name, property => (object?)property.Value, StringComparer.Ordinal);
        properties["notesRevision"] = notes.Revision;
        properties["notes"] = new { notes.Status, notes.Reason, count = notes.Document?.Notes.Length };
        return properties;
    }

    string[] OrphanedNotes(IMcpProposal proposal)
    {
        var notes = ReadNotes(proposal.Before);

        // An unreadable nonsemantic file does not block source proposals and is never rewritten.
        if (notes.Document is null) return [];
        return [.. notes.Document.Notes.Where(note => ModelNotesAnchors.Resolve(note.Anchor, proposal.Before.IdentityCatalog) is not null && ModelNotesAnchors.Resolve(note.Anchor, proposal.Workspace.IdentityCatalog) is null).Select(note => note.Id).Order(StringComparer.Ordinal)];
    }

    ModelNotesRead ReadNotes(ScreenplayWorkspace workspace) => ReadNotes(workspace.IdentityCatalog.Application);

    ModelNotesRead ReadNotes(ApplicationIdentity application)
    {
        try
        {
            return ModelNotesStore.ReadBytes(ReadNotesBytes(), application);
        }
        catch (Exception exception) when (exception is McpFailure or IOException or UnauthorizedAccessException)
        {
            return new("unreadable", null, null, exception.Message);
        }
    }

    byte[]? ReadNotesBytes() => new McpManagedFiles(Root).Read(ModelNotesStore.FileName, ModelNotesStore.MaximumBytes);

    void VerifyNotesPlan(IMcpProposal proposal)
    {
        if (proposal is not McpNotesProposal notes) return;
        var current = ReadNotes(proposal.Before);
        RefuseUnreadableNotes(current);
        if (current.Revision != notes.Notes.BeforeRevision || !McpManagedFiles.Equal(ReadNotesBytes(), notes.Notes.Before))
        {
            throw new McpFailure("Notes changed since the proposal; start a new proposal.") { FailureKind = "NotesRevisionDrift" };
        }
    }
}
