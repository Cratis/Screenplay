// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspace;

public class when_reconciling_unbound_container_additions : given.an_unbound_container_workspace
{
    [Theory]
    [InlineData("deterministic", true)]
    [InlineData("earlierOrigin", false)]
    [InlineData("earlierId", false)]
    [InlineData("documentOrigin", false)]
    [InlineData("newOrigin", false)]
    [InlineData("inSlice", false)]
    [InlineData("eventRevision", false)]
    [InlineData("missingSource", false)]
    public void should_admit_only_the_exact_prebinding_materialization_delta(string delta, bool accepted)
    {
        var original = Workspace.IdentityCatalog;
        var module = original.Semantics.Single(assignment => assignment.Address.Kind == SemanticKind.Module);
        var slice = original.Semantics.Single(assignment => assignment.Address.Kind == SemanticKind.Slice).Address;
        var eventAddress = SemanticAddress.ForEventContract(slice, "OrderPlaced");
        var eventIdentity = new EventContractIdentityAssignment(eventAddress, EventContractId.CreateLegacy(Application, "OrderPlaced"), EventContractRevision.Initial, SemanticIdentityOrigin.LegacyBootstrap);
        var eventSemantic = new SemanticIdentityAssignment(eventAddress, SemanticId.Create(eventAddress), SemanticIdentityOrigin.LegacyBootstrap);
        var earlier = SemanticIdentityCatalog.Create(Application, original.Documents, [module], []);
        var catalog = original;
        if (delta == "earlierOrigin")
        {
            catalog = SemanticIdentityCatalog.Create(Application, original.Documents, [.. original.Semantics.Select(assignment => assignment == module ? assignment with { Origin = SemanticIdentityOrigin.Persisted } : assignment)], []);
        }
        else if (delta == "earlierId")
        {
            catalog = SemanticIdentityCatalog.Create(Application, original.Documents, [.. original.Semantics.Select(assignment => assignment == module ? assignment with { Id = SemanticId.Create(SemanticAddress.ForModule(Application, "External")), Origin = SemanticIdentityOrigin.Persisted } : assignment)], []);
        }
        else if (delta == "documentOrigin")
        {
            catalog = SemanticIdentityCatalog.Create(Application, [.. original.Documents.Select(assignment => assignment with { Origin = SemanticIdentityOrigin.Persisted })], original.Semantics, []);
        }
        else if (delta == "newOrigin")
        {
            catalog = SemanticIdentityCatalog.Create(Application, original.Documents, [.. original.Semantics.Select(assignment => assignment.Address.Equals(slice) ? assignment with { Origin = SemanticIdentityOrigin.Persisted } : assignment)], []);
        }
        else if (delta == "inSlice")
        {
            catalog = SemanticIdentityCatalog.Create(Application, original.Documents, [.. original.Semantics, new(SemanticAddress.ForCommand(slice, "PlaceOrder"), SemanticId.Create(SemanticAddress.ForCommand(slice, "PlaceOrder")), SemanticIdentityOrigin.LegacyBootstrap)], []);
        }
        else if (delta == "eventRevision")
        {
            earlier = SemanticIdentityCatalog.Create(Application, original.Documents, [module, eventSemantic], [eventIdentity]);
            catalog = SemanticIdentityCatalog.Create(Application, original.Documents, [.. original.Semantics, eventSemantic], [eventIdentity with { Revision = new(2) }]);
        }

        var candidate = delta == "missingSource"
            ? Create(Source.Replace("Sales", "Other", StringComparison.Ordinal), catalog)
            : ScreenplayWorkspace.CreateValidated(Workspace.ApplicationName, Workspace.Documents, catalog, Workspace.Compilation);
        WorkspaceContainerIdentities.AllowsReopen(earlier, candidate).ShouldEqual(accepted);
    }
}
