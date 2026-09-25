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
            request.EventRevisionAdvancements.IsDefault ||
            request.RetiredSemanticAddresses.IsDefault || request.RetiredEventAddresses.IsDefault)
        {
            return WorkspaceTransactionOperations.Failure(
                WorkspaceConflictKind.InvalidOperation,
                "Workspace transaction arrays must be non-default.");
        }

        if (request.Operations.IsEmpty && request.SemanticRenames.IsEmpty && request.EventRenames.IsEmpty &&
            request.EventRevisionAdvancements.IsEmpty && request.RetiredSemanticAddresses.IsEmpty && request.RetiredEventAddresses.IsEmpty)
        {
            if (!_workspace.Compilation.Success)
            {
                return WorkspaceTransactionOperations.Failure(
                    WorkspaceConflictKind.CompilationFailed,
                    "An executable-only transaction requires successful semantic compilation, including when no changes are requested.") with
                {
                    Diagnostics = [.. _workspace.Compilation.Diagnostics]
                };
            }

            return Success(_workspace, []);
        }

        var mappingOperations = request.Operations.OfType<UpdateProducedEventMappingSource>().ToArray();
        if (mappingOperations.Length > 0 && (request.Operations.Length != 1 ||
            !request.SemanticRenames.IsEmpty || !request.EventRenames.IsEmpty || !request.EventRevisionAdvancements.IsEmpty ||
            !request.RetiredSemanticAddresses.IsEmpty || !request.RetiredEventAddresses.IsEmpty))
        {
            return WorkspaceTransactionOperations.Failure(
                WorkspaceConflictKind.InvalidOperation,
                "A produced-event mapping patch must be the only operation and cannot include identity migrations.");
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
            return WorkspaceTransactionOperations.CompilationFailure(provisionalCompilation.Diagnostics, ordered);
        }

        SemanticIdentityCatalog migratedCatalog;
        try
        {
            var index = SemanticCompilationIndex.Create(
                provisionalCompilation.Value!.Model.Application,
                _workspace.IdentityCatalog.Application);
            var addresses = index.Declarations.Keys.Order(WorkspaceSemanticAddressComparer.Instance).ToImmutableArray();
            var events = index.Events.Keys.Order(WorkspaceSemanticAddressComparer.Instance).ToImmutableArray();
            var declaredAdvancements = index.Events
                .Where(entry => entry.Value.Revision.Value > 1 &&
                    !_workspace.IdentityCatalog.EventContracts.Any(assignment => assignment.Address.Equals(entry.Key)))
                .Select(entry => new EventContractRevisionAdvancement(entry.Key, entry.Value.Revision)).ToImmutableArray();
            foreach (var advancement in request.EventRevisionAdvancements)
            {
                if (advancement is null || !index.Events.TryGetValue(advancement.Address, out var declaration) ||
                    declaration.Revision != advancement.Revision ||
                    !_workspace.IdentityCatalog.EventContracts.Any(assignment => assignment.Address.Equals(advancement.Address)))
                {
                    throw new InvalidSemanticContract("An event revision advancement must match a persisted event and its declared revision.");
                }
            }

            var movedProperties = _workspace.IdentityCatalog.Semantics
                .Where(assignment => assignment.Address.Kind == SemanticKind.Property &&
                    assignment.Address.OwnerKind == SemanticKind.EventContract &&
                    assignment.Address.Parts[^3].Kind != SemanticAddressPartKind.Generation &&
                    request.EventRevisionAdvancements.Any(advancement =>
                        assignment.Address.Parts[..^2].SequenceEqual(advancement.Address.Parts)))
                .Select(assignment =>
                {
                    var owner = SemanticAddress.FromCanonical(SemanticKind.EventContract, assignment.Address.Parts[..^2]);
                    return new SemanticIdentityRename(
                        assignment.Address,
                        SemanticAddress.ForEventProperty(owner, EventContractRevision.Initial, assignment.Address.Name));
                })
                .Where(rename => addresses.Contains(rename.CurrentAddress)).ToImmutableArray();
            migratedCatalog = SemanticIdentityCatalog.PlanMigration(
                _workspace.IdentityCatalog,
                request.ExpectedCatalogRevision,
                [.. ordered.Select(document => document.StableKey)],
                addresses,
                events,
                [
                    .. documentRenames
                        .OrderBy(rename => rename.PreviousKey, StringComparer.Ordinal)
                        .ThenBy(rename => rename.CurrentKey, StringComparer.Ordinal)
                ],
                [.. request.SemanticRenames, .. movedProperties],
                request.EventRenames,
                [.. retiredDocumentKeys.Order(StringComparer.Ordinal)],
                request.RetiredSemanticAddresses,
                request.RetiredEventAddresses).Catalog;
            var advancements = declaredAdvancements.AddRange(request.EventRevisionAdvancements);
            if (!advancements.IsEmpty)
            {
                migratedCatalog = SemanticIdentityCatalog.PlanEventRevisionAdvancement(
                    migratedCatalog,
                    migratedCatalog.Revision,
                    [.. ordered.Select(document => document.StableKey)],
                    addresses,
                    events,
                    advancements).Catalog;
            }
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
            return WorkspaceTransactionOperations.CompilationFailure(compilation.Diagnostics, ordered);
        }

        var candidate = ScreenplayWorkspace.CreateValidated(
            _workspace.ApplicationName,
            ordered,
            migratedCatalog,
            compilation,
            _workspace.AttachmentContents);
        if (mappingOperations.Length == 1 && ProducedEventMappingPatch.Verify(mappingOperations[0], _workspace, candidate) is { } mappingConflict)
        {
            return WorkspaceTransactionOperations.Failure(mappingConflict);
        }

        return Success(
            candidate,
            WorkspaceTransactionOperations.WriteEntries(_workspace.Documents, candidate.Documents));
    }

    CompilationResult<SemanticCompilation> Compile(
        ImmutableArray<WorkspaceDocument> documents,
        SemanticIdentityCatalog identityCatalog)
    {
        return new SemanticModelCompiler().Compile(
            _workspace.ApplicationName,
            ScreenplayWorkspace.CreateDocumentSet(documents, identityCatalog, _workspace.AttachmentContents));
    }

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
