// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceSyntaxIndex;

public class when_using_existing_catalog_assignments : Specification
{
    ApplicationSyntax _syntax = null!;
    SemanticIdentityCatalog _catalog = null!;
    SemanticAddress _address = null!;
    ImmutableArray<WorkspaceSyntaxEntry> _entries;

    void Establish()
    {
        var application = ApplicationIdentity.Create("Billing");
        _address = SemanticAddress.ForEventContract(SemanticAddress.ForSlice(application, "Billing", "Accounts", "Register"), "Opened");
        _catalog = SemanticIdentityCatalog.Create(
            application,
            [],
            [new(_address, SemanticId.Create(_address), SemanticIdentityOrigin.LegacyBootstrap)],
            [new(_address, EventContractId.CreateLegacy(application, "Opened"), EventContractRevision.Initial, SemanticIdentityOrigin.LegacyBootstrap)]);
        _syntax = new ScreenplayCompiler().Parse("""
            module Billing
              feature Accounts
                slice StateChange Register
                  event Opened
            """).Value!;
    }

    void Because() => _entries = WorkspaceSyntaxIndex.ForSyntax(_syntax, _catalog);

    [Fact] void should_match_structurally_equal_semantic_addresses() => _entries.Single(entry => entry.Node is EventSyntax).SemanticId.ShouldEqual(_catalog.Semantics.Single().Id);
    [Fact] void should_reuse_the_existing_event_contract() => _entries.Single(entry => entry.Node is EventSyntax).EventContractId.ShouldEqual(_catalog.EventContracts.Single().Id);
    [Fact] void should_keep_the_supported_event_address() => _entries.Single(entry => entry.Node is EventSyntax).Address!.Equals(_address).ShouldBeTrue();
    [Fact] void should_not_bootstrap_unassigned_declarations() => _entries.Where(entry => entry.Node is not EventSyntax).All(entry => entry.SemanticId is null && entry.EventContractId is null).ShouldBeTrue();
}
