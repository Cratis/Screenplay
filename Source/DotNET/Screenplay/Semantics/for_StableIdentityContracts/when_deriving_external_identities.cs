// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_StableIdentityContracts;

public class when_deriving_external_identities : Specification
{
    const string StableKey = "studio-artifact-café";
    ApplicationIdentity _application;
    ApplicationIdentity _otherApplication;
    DocumentId _document;
    EventContractId _eventContract;
    EventContractId _legacyEventContract;
    EventContractId _normalizedEventContract;
    EventContractId _otherApplicationEventContract;
    SemanticId[] _kindIdentities;
    SemanticId _addressDerivedSemantic;
    SemanticId _command;
    SemanticId _event;
    SemanticId _normalizedCommand;
    SemanticId _repeatedCommand;

    void Because()
    {
        _application = ApplicationIdentity.Create(StableKey);
        _otherApplication = ApplicationIdentity.Create("another-application");
        _document = DocumentId.Create(StableKey);
        _kindIdentities = [.. Enum.GetValues<SemanticKind>().Where(kind => kind != SemanticKind.Unknown).Select(kind => SemanticId.Create(kind, StableKey))];
        _command = SemanticId.Create(SemanticKind.Command, StableKey);
        _repeatedCommand = SemanticId.Create(SemanticKind.Command, StableKey);
        _event = SemanticId.Create(SemanticKind.EventContract, StableKey);
        _normalizedCommand = SemanticId.Create(SemanticKind.Command, "studio-artifact-cafe\u0301");
        _eventContract = EventContractId.Create(_application, StableKey);
        _normalizedEventContract = EventContractId.Create(_application, "studio-artifact-cafe\u0301");
        _otherApplicationEventContract = EventContractId.Create(_otherApplication, StableKey);
        _legacyEventContract = EventContractId.CreateLegacy(_application, StableKey);
        _addressDerivedSemantic = SemanticId.Create(SemanticAddress.ForCommand(
            SemanticAddress.ForSlice(_application, "Projects", "Registration", "Register"),
            StableKey));
    }

    [Fact] void should_be_deterministic_for_repeated_semantic_creation() => _repeatedCommand.ShouldEqual(_command);
    [Fact] void should_normalize_semantic_keys_to_unicode_nfc() => _normalizedCommand.ShouldEqual(_command);
    [Fact] void should_normalize_event_contract_keys_to_unicode_nfc() => _normalizedEventContract.ShouldEqual(_eventContract);
    [Fact] void should_separate_semantic_kinds() => _event.ShouldNotEqual(_command);
    [Fact] void should_separate_every_semantic_kind() => _kindIdentities.Distinct().Count().ShouldEqual(_kindIdentities.Length);
    [Fact] void should_scope_event_contracts_to_the_application() => _otherApplicationEventContract.ShouldNotEqual(_eventContract);
    [Fact] void should_separate_external_semantics_from_address_derived_semantics() => _addressDerivedSemantic.ShouldNotEqual(_command);
    [Fact] void should_separate_external_event_contracts_from_legacy_event_contracts() => _legacyEventContract.ShouldNotEqual(_eventContract);
    [Fact] void should_domain_separate_every_external_identity_hash() => new[] { Hash(_application.ToString()), Hash(_document.ToString()), Hash(_command.ToString()), Hash(_eventContract.ToString()) }.Distinct(StringComparer.Ordinal).Count().ShouldEqual(4);
    [Fact] void should_use_full_sha256_semantic_identity() => _command.ToString().Length.ShouldEqual(69);
    [Fact] void should_use_full_sha256_event_contract_identity() => _eventContract.ToString().Length.ShouldEqual(69);

    static string Hash(string canonical) => canonical[(canonical.IndexOf(':') + 1)..];
}
