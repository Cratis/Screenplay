// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring.when_repairing_timeline_order;

public class and_document_identities_are_unassigned : given.a_timeline
{
    ScreenplayWorkspace _workspace;
    WorkspaceDiagnosticRepair[] _repairs;
    WorkspaceAuthoringResult? _proposal;

    void Establish()
    {
        var consumer = Slice("View", [], "E").Replace(" optional", "[]", StringComparison.Ordinal).Replace("    by id Uuid\n", string.Empty, StringComparison.Ordinal);
        _workspace = Create(
            ("root.play", "module M\n  feature F\n    import \"*.play\"\n"),
            ("a.play", consumer),
            ("z.play", Slice("Write", ["E"])));
    }

    void Because()
    {
        _repairs = [.. WorkspaceDiagnosticRepairs.Find(_workspace, _workspace.Revision, Finding(_workspace, "E"))];
        _proposal = _repairs.Length == 1 ? Propose(_workspace, _repairs[0]) : null;
    }

    [Fact] void should_start_without_document_assignments() => _workspace.IdentityCatalog.Documents.ShouldBeEmpty();
    [Fact] void should_discover_the_repair() => _repairs.Length.ShouldEqual(1);
    [Fact] void should_accept_the_proposal() => _proposal?.Accepted.ShouldEqual(true);
    [Fact] void should_remove_the_finding() => _proposal!.Workspace!.Compilation.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.EventFromLaterSlice).ShouldBeEmpty();
    [Fact] void should_keep_source_only_readiness() => _proposal!.ExecutableReady.ShouldBeFalse();
    [Fact] void should_assign_only_the_existing_document_identities() => _proposal!.Workspace!.IdentityCatalog.Documents.ShouldEqual(_workspace.Documents.Select(document => new DocumentIdentityAssignment(document.StableKey, document.Id, SemanticIdentityOrigin.Persisted)));

    // Decision 0093 items 1, 3 and 13: exactly the source containers get deterministic identities, never in-slice declarations.
    [Fact] void should_assign_exactly_the_module_feature_and_two_slices() => _proposal!.Workspace!.IdentityCatalog.Semantics.ShouldContainOnly(ExpectedAssignments());
    [Fact] void should_not_assign_in_slice_declarations() => _proposal!.Workspace!.IdentityCatalog.Semantics.All(assignment => assignment.Address.Kind is SemanticKind.Module or SemanticKind.Feature or SemanticKind.Slice).ShouldBeTrue();
    [Fact] void should_not_invent_event_contract_assignments() => _proposal!.Workspace!.IdentityCatalog.EventContracts.ShouldBeEmpty();

    // Decision 0093 item 5: creation materializes the same containers before any proposal and moves the catalog revision.
    [Fact] void should_materialize_exactly_the_same_original_container_assignments() => _workspace.IdentityCatalog.Semantics.ShouldContainOnly(ExpectedAssignments());
    [Fact] void should_move_the_original_catalog_revision() => _workspace.IdentityCatalog.Revision.ShouldNotEqual(SemanticIdentityCatalog.Empty(_workspace.IdentityCatalog.Application).Revision);

    IEnumerable<SemanticIdentityAssignment> ExpectedAssignments()
    {
        var application = _workspace.IdentityCatalog.Application;
        SemanticAddress[] addresses = [SemanticAddress.ForModule(application, "M"), SemanticAddress.ForFeature(application, "M", "F"), SemanticAddress.ForSlice(application, "M", ["F"], "View"), SemanticAddress.ForSlice(application, "M", ["F"], "Write")];
        return addresses.Select(address => new SemanticIdentityAssignment(address, SemanticId.Create(address), SemanticIdentityOrigin.LegacyBootstrap));
    }
}
