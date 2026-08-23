// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Serialization.for_IdentityCatalogSerializer;

public class when_round_tripping_property_owner_kinds : Specification
{
    SemanticIdentityCatalog _roundTripped;

    void Because()
    {
        var application = ApplicationIdentity.Create("Projects");
        var slice = SemanticAddress.ForSlice(application, "Projects", "Projects", "Registration");
        var addresses = new[]
        {
            SemanticAddress.ForProperty(SemanticAddress.ForCommand(slice, "Contract"), "Id"),
            SemanticAddress.ForProperty(SemanticAddress.ForEventContract(slice, "Contract"), "Id"),
            SemanticAddress.ForProperty(SemanticAddress.ForReadModel(slice, "Contract"), "Id")
        };
        var catalog = SemanticIdentityCatalog.Create(
            application,
            [],
            [.. addresses.Select(_ => new SemanticIdentityAssignment(_, SemanticId.Create(_), SemanticIdentityOrigin.LegacyBootstrap))],
            []);
        _roundTripped = SemanticIdentityCatalogSerializer.Deserialize(SemanticIdentityCatalogSerializer.Serialize(catalog));
    }

    [Fact] void should_recover_the_command_owner_kind() => _roundTripped.Semantics.Any(_ => _.Address.OwnerKind == SemanticKind.Command).ShouldBeTrue();
    [Fact] void should_recover_the_event_owner_kind() => _roundTripped.Semantics.Any(_ => _.Address.OwnerKind == SemanticKind.EventContract).ShouldBeTrue();
    [Fact] void should_recover_the_read_model_owner_kind() => _roundTripped.Semantics.Any(_ => _.Address.OwnerKind == SemanticKind.ReadModel).ShouldBeTrue();
    [Fact] void should_preserve_three_distinct_property_addresses() => _roundTripped.Semantics.Select(_ => _.Address).Distinct().Count().ShouldEqual(3);
}
