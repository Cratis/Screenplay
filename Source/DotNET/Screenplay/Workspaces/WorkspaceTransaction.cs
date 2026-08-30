// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces;

sealed class WorkspaceTransaction(ScreenplayWorkspace workspace)
{
    readonly ScreenplayWorkspace _workspace = workspace;

    internal WorkspaceTransactionResult Propose(WorkspaceTransactionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.ExpectedRevision != _workspace.Revision)
        {
            return WorkspaceTransactionOperations.Failure(
                WorkspaceConflictKind.StaleWorkspaceRevision,
                $"Workspace revision '{request.ExpectedRevision}' is stale; current revision is '{_workspace.Revision}'.");
        }

        if (request.ExpectedCatalogRevision != _workspace.IdentityCatalog.Revision)
        {
            return WorkspaceTransactionOperations.Failure(
                WorkspaceConflictKind.StaleCatalogRevision,
                $"Identity-catalog revision '{request.ExpectedCatalogRevision}' is stale; current revision is '{_workspace.IdentityCatalog.Revision}'.");
        }

        if (request.Operations.IsDefault || request.SemanticRenames.IsDefault || request.EventRenames.IsDefault ||
            request.RetiredSemanticAddresses.IsDefault || request.RetiredEventAddresses.IsDefault)
        {
            return WorkspaceTransactionOperations.Failure(
                WorkspaceConflictKind.InvalidOperation,
                "Workspace transaction arrays must be non-default.");
        }

        if (request.Operations.IsEmpty && request.SemanticRenames.IsEmpty && request.EventRenames.IsEmpty &&
            request.RetiredSemanticAddresses.IsEmpty && request.RetiredEventAddresses.IsEmpty)
        {
            return Success(_workspace, []);
        }

        var candidates = _workspace.Documents.ToDictionary(document => document.Id);
        var targeted = new HashSet<DocumentId>();
        var semanticTargeted = new HashSet<DocumentId>();
        var documentRenames = ImmutableArray.CreateBuilder<DocumentIdentityRename>();
        var retiredDocumentKeys = ImmutableArray.CreateBuilder<string>();
        foreach (var operation in request.Operations)
        {
            var conflict = WorkspaceTransactionOperations.Apply(
                operation,
                _workspace,
                candidates,
                targeted,
                semanticTargeted,
                documentRenames,
                retiredDocumentKeys);
            if (conflict is not null)
            {
                return WorkspaceTransactionOperations.Failure(conflict);
            }
        }

        if (candidates.Count == 0)
        {
            return WorkspaceTransactionOperations.Failure(
                WorkspaceConflictKind.InvalidOperation,
                "A workspace transaction cannot remove every source document.");
        }

        var ordered = candidates.Values.OrderBy(document => document.Id.ToString(), StringComparer.Ordinal).ToImmutableArray();
        var collision = WorkspaceTransactionOperations.PortablePathCollision(ordered);
        if (collision is not null)
        {
            return WorkspaceTransactionOperations.Failure(collision);
        }

        if (ordered.Select(document => document.StableKey).Distinct(StringComparer.Ordinal).Count() != ordered.Length)
        {
            return WorkspaceTransactionOperations.Failure(
                WorkspaceConflictKind.InvalidOperation,
                "A workspace transaction produced duplicate stable document keys.");
        }

        CompilationResult<SemanticCompilation> provisionalCompilation;
        try
        {
            var provisionalCatalog = SemanticIdentityCatalog.Create(
                _workspace.IdentityCatalog.Application,
                [
                    .. ordered.Select(document => new DocumentIdentityAssignment(
                        document.StableKey,
                        document.Id,
                        SemanticIdentityOrigin.Persisted))
                ],
                [],
                []);
            provisionalCompilation = Compile(ordered, provisionalCatalog);
        }
        catch (Exception exception) when (exception is InvalidSemanticContract or InvalidScreenplayWorkspace)
        {
            return WorkspaceTransactionOperations.Failure(WorkspaceConflictKind.InvalidOperation, exception.Message);
        }

        if (!provisionalCompilation.Success)
        {
            return WorkspaceTransactionOperations.CompilationFailure(provisionalCompilation.Diagnostics);
        }

        SemanticIdentityCatalog migratedCatalog;
        try
        {
            var index = SemanticCompilationIndex.Create(
                provisionalCompilation.Value!.Model.Application,
                _workspace.IdentityCatalog.Application);
            migratedCatalog = SemanticIdentityCatalog.PlanMigration(
                _workspace.IdentityCatalog,
                request.ExpectedCatalogRevision,
                [.. ordered.Select(document => document.StableKey)],
                [.. index.Declarations.Keys.Order(WorkspaceSemanticAddressComparer.Instance)],
                [.. index.Events.Keys.Order(WorkspaceSemanticAddressComparer.Instance)],
                [
                    .. documentRenames
                        .OrderBy(rename => rename.PreviousKey, StringComparer.Ordinal)
                        .ThenBy(rename => rename.CurrentKey, StringComparer.Ordinal)
                ],
                request.SemanticRenames,
                request.EventRenames,
                [.. retiredDocumentKeys.Order(StringComparer.Ordinal)],
                request.RetiredSemanticAddresses,
                request.RetiredEventAddresses).Catalog;
        }
        catch (InvalidSemanticContract exception)
        {
            return WorkspaceTransactionOperations.Failure(
                WorkspaceConflictKind.InvalidIdentityMigration,
                exception.Message);
        }

        CompilationResult<SemanticCompilation> compilation;
        try
        {
            compilation = Compile(ordered, migratedCatalog);
        }
        catch (InvalidSemanticContract exception)
        {
            return WorkspaceTransactionOperations.Failure(
                WorkspaceConflictKind.InvalidIdentityMigration,
                exception.Message);
        }

        if (!compilation.Success)
        {
            return WorkspaceTransactionOperations.CompilationFailure(compilation.Diagnostics);
        }

        var candidate = ScreenplayWorkspace.CreateValidated(
            _workspace.ApplicationName,
            ordered,
            migratedCatalog,
            compilation);
        return Success(
            candidate,
            WorkspaceTransactionOperations.WriteEntries(_workspace.Documents, candidate.Documents));
    }

    CompilationResult<SemanticCompilation> Compile(
        ImmutableArray<WorkspaceDocument> documents,
        SemanticIdentityCatalog identityCatalog) =>
        new SemanticModelCompiler().Compile(
            _workspace.ApplicationName,
            ScreenplayWorkspace.CreateDocumentSet(documents, identityCatalog));

    WorkspaceTransactionResult Success(
        ScreenplayWorkspace candidate,
        ImmutableArray<WorkspaceWriteEntry> entries) =>
        new()
        {
            Workspace = candidate,
            WritePlan = new WorkspaceWritePlan
            {
                BeforeRevision = _workspace.Revision,
                AfterRevision = candidate.Revision,
                BeforeCatalogRevision = _workspace.IdentityCatalog.Revision,
                AfterCatalogRevision = candidate.IdentityCatalog.Revision,
                Entries = entries
            }
        };
}
