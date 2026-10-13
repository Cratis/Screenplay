// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

/// <summary>
/// Reads committed open model questions using the model root's persisted identity catalog.
/// </summary>
public static class ModelNoteQuestions
{
    /// <summary>
    /// Reads selected questions, reporting all of them as unscoped orphans when identity state is absent.
    /// Unreadable notes are a report failure and never prevent other callers from opening the model.
    /// </summary>
    /// <param name="root">The model root directory.</param>
    /// <param name="scope">Optional case-sensitive dotted module, feature or slice scope.</param>
    /// <returns>The findings, identity and notes statuses, and any read failure.</returns>
    public static ModelNoteQuestionsOutcome Read(string root, string? scope = null)
    {
        var identityStatus = "unavailable";
        string? notesStatus = null;
        var effectiveScope = scope;
        try
        {
            var input = ModelNotesInput.Read(root);
            identityStatus = input.State is null ? "absent" : "persisted";
            effectiveScope = input.State is null ? null : scope;
            var notes = input.Notes;
            notesStatus = notes.Status;
            if (notes.Status == "unreadable") return new([], identityStatus, notes.Status, effectiveScope, input.OrphanReason, "UnreadableNotes", notes.Reason);

            return new(input.Findings(notes.Document!, scope), identityStatus, notes.Status, effectiveScope, input.OrphanReason);
        }
        catch (Exception exception) when (exception is McpFailure or IOException or UnauthorizedAccessException or InvalidSemanticContract or InvalidScreenplayWorkspace or InvalidWorkspaceDocument)
        {
            return new([], identityStatus, notesStatus, effectiveScope, FailureKind: "ReadFailure", Reason: exception.Message);
        }
    }
}
