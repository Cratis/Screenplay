// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.for_StableIdentityContracts;

public class when_deriving_identities : Specification
{
    DocumentId _documentId;
    EventContractId _eventId;
    SemanticId _semanticId;
    SemanticId _relocatedId;
    SemanticId _splitId;
    SemanticId _composedUnicodeId;
    SemanticId _decomposedUnicodeId;
    SemanticId _separatedPartsId;
    SemanticId _separatorTextId;

    void Because()
    {
        var address = SemanticAddress.Create(
            SemanticKind.EventContract,
            [
                SemanticAddressPart.Create(SemanticAddressPartKind.Application, "Projects"),
                SemanticAddressPart.Create(SemanticAddressPartKind.Declaration, "ProjectRegistered")
            ]);
        _documentId = DocumentId.Create("projects-main");
        _semanticId = SemanticId.Create(address);
        _eventId = EventContractId.CreateLegacy(address);
        _relocatedId = SemanticId.Create(address);
        _splitId = SemanticId.Create(address);
        _composedUnicodeId = SemanticId.Create(Address("Café"));
        _decomposedUnicodeId = SemanticId.Create(Address("Cafe\u0301"));
        _separatedPartsId = SemanticId.Create(SemanticAddress.Create(
            SemanticKind.Command,
            [
                SemanticAddressPart.Create(SemanticAddressPartKind.Feature, "a"),
                SemanticAddressPart.Create(SemanticAddressPartKind.Declaration, "b")
            ]));
        _separatorTextId = SemanticId.Create(Address("a|b"));
    }

    [Fact] void should_domain_separate_document_identity() => _documentId.ToString().ShouldNotEqual(_semanticId.ToString());
    [Fact] void should_domain_separate_event_contract_identity() => _eventId.ToString().ShouldNotEqual(_semanticId.ToString());
    [Fact] void should_ignore_relocation() => _relocatedId.ShouldEqual(_semanticId);
    [Fact] void should_ignore_file_splits() => _splitId.ShouldEqual(_semanticId);
    [Fact] void should_normalize_unicode_to_nfc() => _decomposedUnicodeId.ShouldEqual(_composedUnicodeId);
    [Fact] void should_not_collide_parts_with_separator_text() => _separatedPartsId.ShouldNotEqual(_separatorTextId);
    [Fact] void should_use_full_sha256_document_identity() => _documentId.ToString().Length.ShouldEqual(69);
    [Fact] void should_use_full_sha256_semantic_identity() => _semanticId.ToString().Length.ShouldEqual(69);
    [Fact] void should_use_full_sha256_event_identity() => _eventId.ToString().Length.ShouldEqual(69);

    static SemanticAddress Address(string key) => SemanticAddress.Create(
        SemanticKind.Command,
        [SemanticAddressPart.Create(SemanticAddressPartKind.Declaration, key)]);
}
