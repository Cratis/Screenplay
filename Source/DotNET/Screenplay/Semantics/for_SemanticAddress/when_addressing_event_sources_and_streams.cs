// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticAddress;

public class when_addressing_event_sources_and_streams : Specification
{
    [Fact]
    void should_append_kinds_without_changing_existing_values()
    {
        ((int)SemanticKind.Capture).ShouldEqual(16);
        ((int)SemanticKind.EventSource).ShouldEqual(17);
        ((int)SemanticKind.EventStream).ShouldEqual(18);
    }

    [Fact]
    void should_use_an_application_declaration_and_a_source_owned_member()
    {
        var application = ApplicationIdentity.Create("Routes");
        var source = SemanticAddress.ForEventSource(application, "Project");
        var stream = SemanticAddress.ForEventStream(source, "All");
        source.Kind.ShouldEqual(SemanticKind.EventSource);
        source.Parts.Select(part => part.Kind).ToArray().ShouldEqual([SemanticAddressPartKind.Application, SemanticAddressPartKind.Declaration]);
        stream.Parts.Select(part => part.Kind).ToArray().ShouldEqual([SemanticAddressPartKind.Application, SemanticAddressPartKind.Declaration, SemanticAddressPartKind.OwnerKind, SemanticAddressPartKind.Member]);
        stream.Parts[^2].Key.ShouldEqual("17");
        stream.OwnerKind.ShouldEqual(SemanticKind.EventSource);
        SemanticAddress.FromCanonical(stream.Kind, stream.Parts).ShouldEqual(stream);
        SemanticId.Create(stream).ShouldEqual(SemanticId.Create(SemanticAddress.ForEventStream(source, "All")));
        SemanticId.Create(stream).ShouldNotEqual(SemanticId.Create(SemanticAddress.ForEventStream(SemanticAddress.ForEventSource(application, "Other"), "All")));
    }

    [Fact]
    void should_refuse_foreign_owners_and_property_shapes()
    {
        var application = ApplicationIdentity.Create("Routes");
        var concept = SemanticAddress.ForConcept(application, "Project");
        var source = SemanticAddress.ForEventSource(application, "Project");
        var stream = SemanticAddress.ForEventStream(source, "Ledger");
        Catch.Exception(() => SemanticAddress.ForEventStream(concept, "Ledger")).ShouldBeOfExactType<InvalidSemanticContract>();
        Catch.Exception(() => SemanticAddress.ForProperty(source, "Id")).ShouldBeOfExactType<InvalidSemanticContract>();
        Catch.Exception(() => SemanticAddress.FromCanonical(SemanticKind.Property, stream.Parts)).ShouldBeOfExactType<InvalidSemanticContract>();
        Catch.Exception(() => SemanticAddress.FromCanonical(SemanticKind.EventStream, SemanticAddress.ForProperty(SemanticAddress.ForCompositeType(application, "Value"), "Id").Parts)).ShouldBeOfExactType<InvalidSemanticContract>();
    }
}
