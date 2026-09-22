// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces;

static class WorkspaceAuthoringIdentity
{
    internal static SemanticIdentityCatalog Migrate(
        ScreenplayWorkspace workspace,
        WorkspaceAuthoringRequest request,
        ImmutableArray<WorkspaceDocument> documents,
        ApplicationSyntax syntax,
        ImmutableArray<DocumentIdentityRename> documentRenames,
        ImmutableArray<string> retiredDocuments)
    {
        var sourceAddresses = WorkspaceSyntaxAddresses.Declarations(syntax, workspace.IdentityCatalog);
        var documentAssignments = documents.Select(document => new DocumentIdentityAssignment(document.StableKey, document.Id, SemanticIdentityOrigin.Persisted)).ToImmutableArray();
        var provisionalCatalog = SemanticIdentityCatalog.Create(workspace.IdentityCatalog.Application, documentAssignments, [], []);
        var provisional = documents.IsEmpty
            ? ScreenplayWorkspace.EmptyCompilation()
            : new SemanticModelBinder().Bind(workspace.ApplicationName, syntax, ScreenplayWorkspace.CreateDocumentSet(documents, provisionalCatalog));

        ImmutableArray<SemanticAddress> addresses;
        ImmutableArray<SemanticAddress> events;
        if (provisional.Success)
        {
            var index = SemanticCompilationIndex.Create(provisional.Value!.Model.Application, workspace.IdentityCatalog.Application);
            addresses = [.. index.Declarations.Keys.Order(WorkspaceSemanticAddressComparer.Instance)];
            events = [.. index.Events.Keys.Order(WorkspaceSemanticAddressComparer.Instance)];
        }
        else
        {
            // Source correspondence proves continuity without pretending the ESM backend supports new source
            // declarations. No new semantic/event identities are invented while binding is unavailable.
            var retained = workspace.IdentityCatalog.Semantics.Select(assignment => assignment.Address)
                .Concat(request.SemanticRenames.Select(rename => rename.CurrentAddress)).ToHashSet();
            var retainedEvents = workspace.IdentityCatalog.EventContracts.Select(assignment => assignment.Address)
                .Concat(request.EventRenames.Select(rename => rename.CurrentAddress)).ToHashSet();
            addresses = [.. sourceAddresses.Where(retained.Contains)];
            events = [.. sourceAddresses.Where(retainedEvents.Contains)];
        }

        // Failed ESM admission may leave document assignments provisional. Establish those exact existing
        // identities before key migration; the request was already gated against the authoritative base revision.
        var previous = SemanticIdentityCatalog.Create(
            workspace.IdentityCatalog.Application,
            [.. workspace.IdentityCatalog.Documents, .. workspace.Documents
                .Where(document => !workspace.IdentityCatalog.Documents.Any(assignment => assignment.Key == document.StableKey))
                .Select(document => new DocumentIdentityAssignment(document.StableKey, document.Id, SemanticIdentityOrigin.Persisted))],
            workspace.IdentityCatalog.Semantics,
            workspace.IdentityCatalog.EventContracts);
        return SemanticIdentityCatalog.PlanMigration(
            previous,
            previous.Revision,
            [.. documents.Select(document => document.StableKey)],
            addresses,
            events,
            documentRenames,
            request.SemanticRenames,
            request.EventRenames,
            retiredDocuments,
            request.RetiredSemanticAddresses,
            request.RetiredEventAddresses).Catalog;
    }
}
