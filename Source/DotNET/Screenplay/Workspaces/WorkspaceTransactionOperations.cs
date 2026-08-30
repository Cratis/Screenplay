// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces;

static class WorkspaceTransactionOperations
{
    internal static WorkspaceConflict? Apply(
        WorkspaceOperation? operation,
        Dictionary<DocumentId, WorkspaceDocument> candidates,
        HashSet<DocumentId> targeted,
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
                ReplaceWorkspaceDocument replace => Replace(replace, candidates, targeted),
                MoveWorkspaceDocument move => Move(move, candidates, targeted),
                RenameWorkspaceDocument rename => Rename(rename, candidates, targeted, documentRenames),
                RemoveWorkspaceDocument remove => Remove(remove, candidates, targeted, retiredDocumentKeys),
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
        HashSet<DocumentId> targeted)
    {
        var conflict = Existing(operation.Document, candidates, targeted, out var current);
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
        HashSet<DocumentId> targeted)
    {
        var conflict = Existing(operation.Document, candidates, targeted, out var current);
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
        ImmutableArray<DocumentIdentityRename>.Builder documentRenames)
    {
        var conflict = Existing(operation.Document, candidates, targeted, out var current);
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
        ImmutableArray<string>.Builder retiredDocumentKeys)
    {
        var conflict = Existing(operation.Document, candidates, targeted, out var current);
        if (conflict is not null)
        {
            return conflict;
        }

        retiredDocumentKeys.Add(current!.StableKey);
        candidates.Remove(operation.Document);
        return null;
    }

    static WorkspaceConflict? Existing(
        DocumentId id,
        Dictionary<DocumentId, WorkspaceDocument> candidates,
        HashSet<DocumentId> targeted,
        out WorkspaceDocument? current)
    {
        current = null;
        if (!id.IsSet || !targeted.Add(id))
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
