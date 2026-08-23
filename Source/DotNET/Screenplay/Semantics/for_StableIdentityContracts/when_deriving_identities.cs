// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_StableIdentityContracts;

public class when_deriving_identities : Specification
{
    ApplicationIdentity _application;
    ApplicationIdentity _otherApplication;
    DocumentId _documentId;
    EventContractId _eventId;
    EventContractId _otherApplicationEventId;
    SemanticId _semanticId;
    SemanticId _relocatedId;
    SemanticId _splitId;
    SemanticId _composedUnicodeId;
    SemanticId _decomposedUnicodeId;
    SemanticId _separatedPartsId;
    SemanticId _separatorTextId;

    void Because()
    {
        _application = ApplicationIdentity.Create("Projects");
        _otherApplication = ApplicationIdentity.Create("OtherProjects");
        var address = EventAddress(_application, "ProjectRegistered");
        _documentId = DocumentId.Create("projects-main");
        _semanticId = SemanticId.Create(address);
        _eventId = EventContractId.CreateLegacy(_application, address.Name);
        _otherApplicationEventId = EventContractId.CreateLegacy(_otherApplication, address.Name);
        _relocatedId = SemanticId.Create(address);
        _splitId = SemanticId.Create(address);
        _composedUnicodeId = SemanticId.Create(CommandAddress(_application, "Café"));
        _decomposedUnicodeId = SemanticId.Create(CommandAddress(_application, "Cafe\u0301"));
        _separatedPartsId = SemanticId.Create(SemanticAddress.ForCommand(
            SemanticAddress.ForSlice(_application, "Projects", "a", "Registration"),
            "b"));
        _separatorTextId = SemanticId.Create(CommandAddress(_application, "a|b"));
    }

    [Fact] void should_domain_separate_application_identity() => _application.ToString().ShouldNotEqual(_semanticId.ToString());
    [Fact] void should_domain_separate_document_identity() => _documentId.ToString().ShouldNotEqual(_semanticId.ToString());
    [Fact] void should_domain_separate_event_contract_identity() => _eventId.ToString().ShouldNotEqual(_semanticId.ToString());
    [Fact] void should_scope_legacy_event_identity_to_the_application() => _otherApplicationEventId.ShouldNotEqual(_eventId);
    [Fact] void should_ignore_relocation() => _relocatedId.ShouldEqual(_semanticId);
    [Fact] void should_ignore_file_splits() => _splitId.ShouldEqual(_semanticId);
    [Fact] void should_normalize_unicode_to_nfc() => _decomposedUnicodeId.ShouldEqual(_composedUnicodeId);
    [Fact] void should_not_collide_parts_with_separator_text() => _separatedPartsId.ShouldNotEqual(_separatorTextId);
    [Fact] void should_use_full_sha256_application_identity() => _application.ToString().Length.ShouldEqual(69);
    [Fact] void should_use_full_sha256_document_identity() => _documentId.ToString().Length.ShouldEqual(69);
    [Fact] void should_use_full_sha256_semantic_identity() => _semanticId.ToString().Length.ShouldEqual(69);
    [Fact] void should_use_full_sha256_event_identity() => _eventId.ToString().Length.ShouldEqual(69);

    static SemanticAddress CommandAddress(ApplicationIdentity application, string name) => SemanticAddress.ForCommand(
        SemanticAddress.ForSlice(application, "Projects", "Projects", "Registration"),
        name);

    static SemanticAddress EventAddress(ApplicationIdentity application, string name) => SemanticAddress.ForEventContract(
        SemanticAddress.ForSlice(application, "Projects", "Projects", "Registration"),
        name);
}
