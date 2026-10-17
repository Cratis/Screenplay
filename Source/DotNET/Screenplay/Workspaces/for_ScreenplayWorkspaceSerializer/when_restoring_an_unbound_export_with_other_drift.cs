// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspaceSerializer;

public class when_restoring_an_unbound_export_with_other_drift : for_ScreenplayWorkspace.given.an_unbound_container_workspace
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_refuse_missing_earlier_assignments_despite_deterministic_additions(bool eventContract)
    {
        var absent = SemanticAddress.ForSlice(Application, "Sales", "Orders", "Removed");
        var address = eventContract ? SemanticAddress.ForEventContract(absent, "Removed") : absent;
        var catalog = SemanticIdentityCatalog.Create(
            Application,
            Workspace.IdentityCatalog.Documents,
            [new(address, SemanticId.Create(address), SemanticIdentityOrigin.LegacyBootstrap)],
            eventContract ? [new(address, EventContractId.CreateLegacy(Application, "Removed"), EventContractRevision.Initial, SemanticIdentityOrigin.LegacyBootstrap)] : []);
        var legacy = ScreenplayWorkspace.CreateValidated(Workspace.ApplicationName, Workspace.Documents, catalog, Workspace.Compilation);
        var error = Catch.Exception(() => ScreenplayWorkspaceSerializer.Deserialize(ScreenplayWorkspaceSerializer.Serialize(legacy)));
        error.ShouldBeOfExactType<InvalidScreenplayWorkspace>();
        error.Message.ShouldEqual("Workspace admission would change the supplied authoritative identity catalog.");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_not_waive_legacy_revision_or_canonical_validation(bool nonCanonical)
    {
        var catalog = SemanticIdentityCatalog.Create(Application, Workspace.IdentityCatalog.Documents, [], []);
        var legacy = ScreenplayWorkspace.CreateValidated(Workspace.ApplicationName, Workspace.Documents, catalog, Workspace.Compilation);
        var json = Encoding.UTF8.GetString(ScreenplayWorkspaceSerializer.Serialize(legacy));
        json = nonCanonical ? json + " " : json.Replace(legacy.Revision.ToString(), Workspace.Revision.ToString(), StringComparison.Ordinal);
        var error = Catch.Exception(() => ScreenplayWorkspaceSerializer.Deserialize(Encoding.UTF8.GetBytes(json)));
        error.ShouldBeOfExactType<InvalidScreenplayWorkspace>();
        error.Message.ShouldEqual(nonCanonical ? "Workspace JSON is valid but not canonical." : "The supplied workspace revision does not match its exact content.");
    }
}
