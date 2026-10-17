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
            : new SemanticModelBinder().Bind(workspace.ApplicationName, syntax, ScreenplayWorkspace.CreateDocumentSet(documents, provisionalCatalog, workspace.AttachmentContents));

        ImmutableArray<SemanticAddress> addresses;
        ImmutableArray<SemanticAddress> events;
        ImmutableArray<EventContractRevisionAdvancement> newGenerations = [];
        if (provisional.Success)
        {
            var index = SemanticCompilationIndex.Create(provisional.Value!.Model.Application, workspace.IdentityCatalog.Application);
            addresses = [.. index.Declarations.Keys.Order(WorkspaceSemanticAddressComparer.Instance)];
            events = [.. index.Events.Keys.Order(WorkspaceSemanticAddressComparer.Instance)];
            newGenerations = [.. index.Events
                .Where(entry => entry.Value.Revision.Value > 1 &&
                    !workspace.IdentityCatalog.EventContracts.Any(assignment => assignment.Address.Equals(entry.Key)) &&
                    !request.EventRenames.Any(rename => rename.CurrentAddress.Equals(entry.Key)))
                .Select(entry => new EventContractRevisionAdvancement(entry.Key, entry.Value.Revision))];
        }
        else
        {
            // Source correspondence retains bound assignments. Only name-based containers can gain
            // deterministic identities before binding; event contracts still require the bound generations.
            var retained = workspace.IdentityCatalog.Semantics.Select(assignment => assignment.Address)
                .Concat(request.SemanticRenames.Select(rename => rename.CurrentAddress)).ToHashSet();
            var retainedEvents = workspace.IdentityCatalog.EventContracts.Select(assignment => assignment.Address)
                .Concat(request.EventRenames.Select(rename => rename.CurrentAddress)).ToHashSet();
            addresses = [.. sourceAddresses.Where(retained.Contains)];
            events = [.. sourceAddresses.Where(retainedEvents.Contains)];
        }

        RequireAdvancements(workspace, request, syntax);
        var movedProperties = InitialPropertyMigrations(workspace, request, addresses);

        // Failed ESM admission may leave document assignments provisional. Establish those exact existing
        // identities before key migration; the request was already gated against the authoritative base revision.
        var previous = SemanticIdentityCatalog.Create(
            workspace.IdentityCatalog.Application,
            [.. workspace.IdentityCatalog.Documents, .. workspace.Documents
                .Where(document => !workspace.IdentityCatalog.Documents.Any(assignment => assignment.Key == document.StableKey))
                .Select(document => new DocumentIdentityAssignment(document.StableKey, document.Id, SemanticIdentityOrigin.Persisted))],
            workspace.IdentityCatalog.Semantics,
            workspace.IdentityCatalog.EventContracts);
        var keys = documents.Select(document => document.StableKey).ToImmutableArray();
        var catalog = SemanticIdentityCatalog.PlanMigration(
            previous,
            previous.Revision,
            keys,
            addresses,
            events,
            documentRenames,
            [.. request.SemanticRenames, .. movedProperties],
            request.EventRenames,
            retiredDocuments,
            request.RetiredSemanticAddresses,
            request.RetiredEventAddresses).Catalog;
        var advancements = newGenerations.AddRange(request.EventRevisionAdvancements);
        if (!advancements.IsEmpty)
        {
            catalog = SemanticIdentityCatalog.PlanEventRevisionAdvancement(
                catalog, catalog.Revision, keys, addresses, events, advancements).Catalog;
        }

        if (!provisional.Success && !documents.IsEmpty)
        {
            var candidate = ScreenplayWorkspace.CreateValidated(workspace.ApplicationName, documents, catalog, provisional);
            catalog = new WorkspaceContainerIdentities(WorkspaceSyntaxIndex.Create(candidate)).Materialize(catalog);
        }

        return catalog;
    }

    static ImmutableArray<SemanticIdentityRename> InitialPropertyMigrations(ScreenplayWorkspace workspace, WorkspaceAuthoringRequest request, ImmutableArray<SemanticAddress> addresses) =>
        [.. workspace.IdentityCatalog.Semantics
            .Where(assignment => assignment.Address.Kind == SemanticKind.Property && assignment.Address.OwnerKind == SemanticKind.EventContract &&
                assignment.Address.Parts[^3].Kind != SemanticAddressPartKind.Generation && request.EventRevisionAdvancements.Any(advancement =>
                    assignment.Address.Parts[..^2].SequenceEqual(advancement.Address.Parts) || request.EventRenames.Any(rename => rename.PreviousAddress.Parts.SequenceEqual(assignment.Address.Parts[..^2]) && rename.CurrentAddress.Equals(advancement.Address))))
            .Select(assignment => InitialPropertyRename(assignment.Address, request))
            .Where(rename => addresses.Contains(rename.CurrentAddress) && !request.SemanticRenames.Any(explicitRename => explicitRename.PreviousAddress.Equals(rename.PreviousAddress)))];

    static SemanticIdentityRename InitialPropertyRename(SemanticAddress property, WorkspaceAuthoringRequest request)
    {
        var owner = SemanticAddress.FromCanonical(SemanticKind.EventContract, property.Parts[..^2]);
        var renamedOwner = request.EventRenames.FirstOrDefault(rename => rename.PreviousAddress.Equals(owner))?.CurrentAddress ?? owner;
        return new(property, SemanticAddress.ForEventProperty(renamedOwner, EventContractRevision.Initial, property.Name));
    }

    static bool MatchesDeclaredRevision(Dictionary<SemanticAddress, uint> events, EventContractRevisionAdvancement advancement) =>
        events.TryGetValue(advancement.Address, out var revision) && revision == advancement.Revision.Value;

    static bool AdvancesPersistedEvent(ScreenplayWorkspace workspace, WorkspaceAuthoringRequest request, SemanticAddress address)
    {
        bool Persisted(SemanticAddress owner) => workspace.IdentityCatalog.EventContracts.Any(assignment => assignment.Address.Equals(owner));
        return Persisted(address) || request.EventRenames.Any(rename => rename.CurrentAddress.Equals(address) && Persisted(rename.PreviousAddress));
    }

    static void RequireAdvancements(ScreenplayWorkspace workspace, WorkspaceAuthoringRequest request, ApplicationSyntax syntax)
    {
        var events = WorkspaceSyntaxIndex.ForSyntax(syntax, workspace.IdentityCatalog)
            .Where(entry => entry.Node is EventSyntax && entry.Address is not null)
            .GroupBy(entry => entry.Address!).ToDictionary(group => group.Key, group => group.Max(entry => ((EventSyntax)entry.Node).Generation));
        foreach (var advancement in request.EventRevisionAdvancements)
        {
            if (!MatchesDeclaredRevision(events, advancement) || !AdvancesPersistedEvent(workspace, request, advancement.Address))
            {
                throw new InvalidSemanticContract("An event revision advancement must match a persisted event and its declared revision.");
            }
        }
    }
}
