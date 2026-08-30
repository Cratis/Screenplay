// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Workspaces;

static class WorkspaceTransactionOperations
{
    static readonly UTF8Encoding _strictUtf8 = new(false, true);
    static readonly byte[] _utf8Bom = [0xef, 0xbb, 0xbf];

    internal static WorkspaceConflict? Apply(
        WorkspaceOperation? operation,
        ScreenplayWorkspace workspace,
        Dictionary<DocumentId, WorkspaceDocument> candidates,
        HashSet<DocumentId> targeted,
        HashSet<DocumentId> semanticTargeted,
        ImmutableArray<DocumentIdentityRename>.Builder documentRenames,
        ImmutableArray<string>.Builder retiredDocumentKeys)
    {
        if (operation is null)
        {
            return Conflict(WorkspaceConflictKind.InvalidOperation, "A workspace transaction contains a null operation.");
        }

        try
        {
            return operation switch
            {
                AddWorkspaceDocument add => Add(add, candidates, targeted),
                ReplaceWorkspaceDocument replace => Replace(replace, candidates, targeted, semanticTargeted),
                MoveWorkspaceDocument move => Move(move, candidates, targeted, semanticTargeted),
                RenameWorkspaceDocument rename => Rename(rename, candidates, targeted, semanticTargeted, documentRenames),
                RemoveWorkspaceDocument remove => Remove(remove, candidates, targeted, semanticTargeted, retiredDocumentKeys),
                UpdateSliceDescription update => UpdateSliceDescription(update, workspace, candidates, targeted, semanticTargeted),
                _ => Conflict(WorkspaceConflictKind.InvalidOperation, $"Workspace operation '{operation.GetType().Name}' is not supported.")
            };
        }
        catch (Exception exception) when (exception is InvalidWorkspaceDocument or InvalidPortablePlayPath or InvalidSemanticContract)
        {
            return Conflict(WorkspaceConflictKind.InvalidOperation, exception.Message);
        }
    }

    internal static WorkspaceConflict? PortablePathCollision(ImmutableArray<WorkspaceDocument> documents)
    {
        var paths = new Dictionary<PortablePlayPath, WorkspaceDocument>(PortablePlayPath.CollisionComparer);
        foreach (var document in documents)
        {
            if (paths.TryGetValue(document.Path, out var existing))
            {
                return Conflict(
                    WorkspaceConflictKind.PortablePathCollision,
                    $"Workspace paths '{existing.Path}' and '{document.Path}' collide on a portable file system.",
                    document.Id,
                    document.Path);
            }

            paths.Add(document.Path, document);
        }

        return null;
    }

    internal static WorkspaceTransactionResult CompilationFailure(IEnumerable<Diagnostic> diagnostics) => new()
    {
        Conflicts =
        [
            Conflict(
                WorkspaceConflictKind.CompilationFailed,
                "The candidate workspace did not compile as one coherent Screenplay application.")
        ],
        Diagnostics = [.. diagnostics]
    };

    internal static WorkspaceTransactionResult Failure(
        WorkspaceConflictKind kind,
        string message) => Failure(Conflict(kind, message));

    internal static WorkspaceTransactionResult Failure(WorkspaceConflict conflict) => new()
    {
        Conflicts = [conflict]
    };

    internal static ImmutableArray<WorkspaceWriteEntry> WriteEntries(
        ImmutableArray<WorkspaceDocument> before,
        ImmutableArray<WorkspaceDocument> after)
    {
        var previous = before.ToDictionary(document => document.Id);
        var current = after.ToDictionary(document => document.Id);
        var ids = previous.Keys.Concat(current.Keys).Distinct().OrderBy(id => id.ToString(), StringComparer.Ordinal);
        var entries = ImmutableArray.CreateBuilder<WorkspaceWriteEntry>();
        foreach (var id in ids)
        {
            previous.TryGetValue(id, out var oldDocument);
            current.TryGetValue(id, out var newDocument);
            if (Same(oldDocument, newDocument))
            {
                continue;
            }

            entries.Add(new WorkspaceWriteEntry
            {
                Document = id,
                Kind = Kind(oldDocument, newDocument),
                Before = oldDocument,
                After = newDocument
            });
        }

        return entries.ToImmutable();
    }

    static WorkspaceConflict? Add(
        AddWorkspaceDocument operation,
        Dictionary<DocumentId, WorkspaceDocument> candidates,
        HashSet<DocumentId> targeted)
    {
        if (operation.Bytes.IsDefault)
        {
            return Conflict(WorkspaceConflictKind.InvalidOperation, "Added workspace document bytes must be non-default.");
        }

        var document = WorkspaceDocument.Create(operation.StableKey, operation.Path, operation.Bytes.AsSpan());
        if (!targeted.Add(document.Id) || candidates.ContainsKey(document.Id))
        {
            return Conflict(
                WorkspaceConflictKind.InvalidOperation,
                $"Workspace document '{document.Id}' is added or targeted more than once.",
                document.Id,
                document.Path);
        }

        candidates.Add(document.Id, document);
        return null;
    }

    static WorkspaceConflict? Replace(
        ReplaceWorkspaceDocument operation,
        Dictionary<DocumentId, WorkspaceDocument> candidates,
        HashSet<DocumentId> targeted,
        HashSet<DocumentId> semanticTargeted)
    {
        var conflict = Existing(operation.Document, candidates, targeted, semanticTargeted, out var current);
        if (conflict is not null)
        {
            return conflict;
        }

        if (operation.Bytes.IsDefault)
        {
            return Conflict(WorkspaceConflictKind.InvalidOperation, "Replacement workspace document bytes must be non-default.");
        }

        if (!current!.Bytes.AsSpan().SequenceEqual(operation.Bytes.AsSpan()))
        {
            candidates[operation.Document] = WorkspaceDocument.Create(
                current.Id,
                current.StableKey,
                current.Path,
                operation.Bytes.AsSpan());
        }

        return null;
    }

    static WorkspaceConflict? Move(
        MoveWorkspaceDocument operation,
        Dictionary<DocumentId, WorkspaceDocument> candidates,
        HashSet<DocumentId> targeted,
        HashSet<DocumentId> semanticTargeted)
    {
        var conflict = Existing(operation.Document, candidates, targeted, semanticTargeted, out var current);
        if (conflict is not null)
        {
            return conflict;
        }

        if (operation.Path is null)
        {
            return Conflict(WorkspaceConflictKind.InvalidOperation, "A moved workspace document requires a portable path.");
        }

        if (current!.Path != operation.Path)
        {
            candidates[operation.Document] = WorkspaceDocument.Create(
                current.Id,
                current.StableKey,
                operation.Path,
                current.Bytes.AsSpan());
        }

        return null;
    }

    static WorkspaceConflict? Rename(
        RenameWorkspaceDocument operation,
        Dictionary<DocumentId, WorkspaceDocument> candidates,
        HashSet<DocumentId> targeted,
        HashSet<DocumentId> semanticTargeted,
        ImmutableArray<DocumentIdentityRename>.Builder documentRenames)
    {
        var conflict = Existing(operation.Document, candidates, targeted, semanticTargeted, out var current);
        if (conflict is not null)
        {
            return conflict;
        }

        var renamed = WorkspaceDocument.Create(
            current!.Id,
            operation.StableKey,
            current.Path,
            current.Bytes.AsSpan());
        if (!string.Equals(current.StableKey, renamed.StableKey, StringComparison.Ordinal))
        {
            candidates[operation.Document] = renamed;
            documentRenames.Add(new(current.StableKey, renamed.StableKey));
        }

        return null;
    }

    static WorkspaceConflict? Remove(
        RemoveWorkspaceDocument operation,
        Dictionary<DocumentId, WorkspaceDocument> candidates,
        HashSet<DocumentId> targeted,
        HashSet<DocumentId> semanticTargeted,
        ImmutableArray<string>.Builder retiredDocumentKeys)
    {
        var conflict = Existing(operation.Document, candidates, targeted, semanticTargeted, out var current);
        if (conflict is not null)
        {
            return conflict;
        }

        retiredDocumentKeys.Add(current!.StableKey);
        candidates.Remove(operation.Document);
        return null;
    }

    static WorkspaceConflict? UpdateSliceDescription(
        UpdateSliceDescription operation,
        ScreenplayWorkspace workspace,
        Dictionary<DocumentId, WorkspaceDocument> candidates,
        HashSet<DocumentId> targeted,
        HashSet<DocumentId> semanticTargeted)
    {
        if (!workspace.Compilation.Success || workspace.Compilation.Value is null)
        {
            return Conflict(
                WorkspaceConflictKind.CompilationFailed,
                "Semantic patches require a successfully compiled workspace source map.");
        }

        if (operation.ExpectedCurrentDescription is null || operation.NewDescription is null)
        {
            return Conflict(
                WorkspaceConflictKind.InvalidOperation,
                "A slice-description patch requires non-null expected and replacement values.");
        }

        var assignment = workspace.IdentityCatalog.Semantics.FirstOrDefault(value => value.Id == operation.SemanticId);
        if (assignment is null)
        {
            return Conflict(
                WorkspaceConflictKind.SemanticIdNotFound,
                $"Semantic identity '{operation.SemanticId}' does not exist in the workspace identity catalog.");
        }

        if (assignment.Address.Kind != SemanticKind.Slice)
        {
            return Conflict(
                WorkspaceConflictKind.UnsupportedSemanticField,
                $"Semantic identity '{operation.SemanticId}' does not address a slice description.");
        }

        var descriptionEntries = workspace.Compilation.Value.SourceMap.Entries
            .Where(entry => entry.SemanticId == operation.SemanticId && entry.Role == SemanticSourceMapRole.Description)
            .Take(2)
            .ToArray();
        if (descriptionEntries.Length == 0)
        {
            return Conflict(
                WorkspaceConflictKind.UnsupportedSemanticField,
                $"Slice '{operation.SemanticId}' has no single-line quoted description available to patch.");
        }

        if (descriptionEntries.Length > 1)
        {
            return Conflict(
                WorkspaceConflictKind.MultiOwnerSemanticEdit,
                $"Slice '{operation.SemanticId}' has more than one description source owner.");
        }

        var descriptionEntry = descriptionEntries[0];
        var documentId = descriptionEntry.Span.Document;
        if (targeted.Contains(documentId))
        {
            return Conflict(
                WorkspaceConflictKind.MultiOwnerSemanticEdit,
                $"Workspace document '{documentId}' is already targeted by another operation in this transaction.",
                documentId);
        }

        if (!candidates.TryGetValue(documentId, out var current))
        {
            return Conflict(
                WorkspaceConflictKind.SemanticIdNotFound,
                $"Workspace document '{documentId}' owning slice '{operation.SemanticId}' does not exist.",
                documentId);
        }

        var span = descriptionEntry.Span;
        var rawBody = current.Text.Substring(span.Start, span.Length);
        var currentDescription = StringLiteral.Unescape(rawBody);
        if (!string.Equals(currentDescription, operation.ExpectedCurrentDescription, StringComparison.Ordinal))
        {
            return Conflict(
                WorkspaceConflictKind.SemanticFieldValueDrift,
                $"Slice '{operation.SemanticId}' description does not match the expected current value.",
                documentId);
        }

        SemanticDocumentText.RequireWellFormedUnicode(operation.NewDescription, "new slice description");
        var escapedNewDescription = StringLiteral.Escape(operation.NewDescription);
        var newText = string.Concat(
            current.Text.AsSpan(0, span.Start),
            escapedNewDescription,
            current.Text.AsSpan(span.Start + span.Length));
        var bytes = EncodeStrictUtf8(newText, current.Encoding == WorkspaceTextEncoding.Utf8WithBom);
        if (!current.Bytes.AsSpan().SequenceEqual(bytes.AsSpan()))
        {
            candidates[documentId] = WorkspaceDocument.Create(current.Id, current.StableKey, current.Path, bytes.AsSpan());
        }

        targeted.Add(documentId);
        semanticTargeted.Add(documentId);
        return null;
    }

    static ImmutableArray<byte> EncodeStrictUtf8(string text, bool withBom)
    {
        var byteCount = _strictUtf8.GetByteCount(text);
        var prefixLength = withBom ? _utf8Bom.Length : 0;
        var bytes = new byte[prefixLength + byteCount];
        if (withBom)
        {
            _utf8Bom.CopyTo(bytes.AsSpan());
        }

        _strictUtf8.GetBytes(text, bytes.AsSpan(prefixLength));
        return ImmutableArray.Create(bytes);
    }

    static WorkspaceConflict? Existing(
        DocumentId id,
        Dictionary<DocumentId, WorkspaceDocument> candidates,
        HashSet<DocumentId> targeted,
        HashSet<DocumentId> semanticTargeted,
        out WorkspaceDocument? current)
    {
        current = null;
        if (!id.IsSet)
        {
            return Conflict(
                WorkspaceConflictKind.InvalidOperation,
                $"Workspace document '{id}' is unset or targeted more than once.",
                id);
        }

        if (semanticTargeted.Contains(id))
        {
            return Conflict(
                WorkspaceConflictKind.MultiOwnerSemanticEdit,
                $"Workspace document '{id}' is already claimed by a semantic patch in this transaction.",
                id);
        }

        if (!targeted.Add(id))
        {
            return Conflict(
                WorkspaceConflictKind.InvalidOperation,
                $"Workspace document '{id}' is unset or targeted more than once.",
                id);
        }

        if (!candidates.TryGetValue(id, out current))
        {
            return Conflict(WorkspaceConflictKind.InvalidOperation, $"Workspace document '{id}' does not exist.", id);
        }

        return null;
    }

    static WorkspaceConflict Conflict(
        WorkspaceConflictKind kind,
        string message,
        DocumentId? document = null,
        PortablePlayPath? path = null) => new()
        {
            Kind = kind,
            Message = message,
            Document = document,
            Path = path
        };

    static bool Same(WorkspaceDocument? before, WorkspaceDocument? after) =>
        ReferenceEquals(before, after) ||
        (before is not null && after is not null &&
         before.Id == after.Id &&
         string.Equals(before.StableKey, after.StableKey, StringComparison.Ordinal) &&
         before.Path == after.Path &&
         before.Encoding == after.Encoding &&
         before.Bytes.AsSpan().SequenceEqual(after.Bytes.AsSpan()));

    static WorkspaceWriteKind Kind(WorkspaceDocument? before, WorkspaceDocument? after)
    {
        if (before is null)
        {
            return WorkspaceWriteKind.Added;
        }

        if (after is null)
        {
            return WorkspaceWriteKind.Removed;
        }

        if (!string.Equals(before.StableKey, after.StableKey, StringComparison.Ordinal))
        {
            return WorkspaceWriteKind.Renamed;
        }

        return before.Path != after.Path
            ? WorkspaceWriteKind.Moved
            : WorkspaceWriteKind.Replaced;
    }
}
