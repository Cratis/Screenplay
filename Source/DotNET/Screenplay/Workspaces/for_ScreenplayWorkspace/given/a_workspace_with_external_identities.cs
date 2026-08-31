// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspace.given;

public class a_workspace_with_external_identities : a_valid_workspace
{
    protected ApplicationIdentity StableApplicationIdentity;
    protected EventContractId EventContractIdentity;
    protected SemanticAddress ApplicationAddress = null!;
    protected SemanticAddress CommandAddress = null!;
    protected SemanticAddress EventAddress = null!;
    protected SemanticId ApplicationSemanticIdentity;
    protected SemanticId CommandSemanticIdentity;
    protected SemanticId EventSemanticIdentity;

    void Establish()
    {
        StableApplicationIdentity = ApplicationIdentity.Create("studio-application-42");
        ApplicationAddress = SemanticAddress.ForApplication(StableApplicationIdentity);
        var sliceAddress = SemanticAddress.ForSlice(StableApplicationIdentity, "Projects", "Registration", "RegisterProject");
        CommandAddress = SemanticAddress.ForCommand(sliceAddress, "RegisterProject");
        EventAddress = SemanticAddress.ForEventContract(sliceAddress, "ProjectRegistered");
        ApplicationSemanticIdentity = SemanticId.Create(SemanticKind.Application, "studio-application-node-42");
        CommandSemanticIdentity = SemanticId.Create(SemanticKind.Command, "studio-command-node-42");
        EventSemanticIdentity = SemanticId.Create(SemanticKind.EventContract, "studio-event-node-42");
        EventContractIdentity = EventContractId.Create(StableApplicationIdentity, "studio-event-contract-42");
        var catalog = SemanticIdentityCatalog.Create(
            StableApplicationIdentity,
            [],
            [
                new(ApplicationAddress, ApplicationSemanticIdentity, SemanticIdentityOrigin.Persisted),
                new(CommandAddress, CommandSemanticIdentity, SemanticIdentityOrigin.Persisted),
                new(EventAddress, EventSemanticIdentity, SemanticIdentityOrigin.Persisted)
            ],
            [new(EventAddress, EventContractIdentity, EventContractRevision.Initial, SemanticIdentityOrigin.Persisted)]);
        Workspace = ScreenplayWorkspace.Create(
            StableApplicationIdentity,
            "Projects",
            [Registration, Concepts],
            catalog);
    }
}
