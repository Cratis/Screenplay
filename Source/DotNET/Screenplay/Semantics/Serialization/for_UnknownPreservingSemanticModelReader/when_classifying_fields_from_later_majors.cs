// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using System.Text.Json.Nodes;

namespace Cratis.Screenplay.Semantics.Serialization.for_UnknownPreservingSemanticModelReader;

public class when_classifying_fields_from_later_majors : Specification
{
    UnknownPreservingSemanticDocument _older;
    UnknownPreservingSemanticDocument _handler;
    UnknownPreservingSemanticDocument _newer;
    byte[] _input;
    SemanticId _command;
    Exception? _registeredRefusal;

    void Because()
    {
        var root = given.documents.Future();
        root["application"]!["eventSources"] = JsonNode.Parse("{\"a\":[null],\"z\":{\"id\":\"not-an-identity\",\"policies\":true}}");
        _input = given.documents.Seal(root);
        _older = UnknownPreservingSemanticModelReader.Read(_input, given.documents.Range());
        root["languageVersion"] = "1.0";
        root["semanticVersion"] = "1.0";
        _registeredRefusal = Catch.Exception(() => UnknownPreservingSemanticModelReader.Read(given.documents.Seal(root), given.documents.Range()));

        var beforeHandler = given.documents.Future(22);
        var command = given.documents.Find(beforeHandler, "destination").Parent!.AsObject();
        _command = SemanticId.Parse(command["id"]!.GetValue<string>());
        command["handler"] = JsonNode.Parse("{\"a\":false,\"id\":\"not-an-identity\",\"z\":[null]}");
        _handler = UnknownPreservingSemanticModelReader.Read(given.documents.Seal(beforeHandler), given.documents.Range(22));

        var atHandler = given.documents.Future(23);
        given.documents.Find(atHandler, "handler")["future"] = true;
        _newer = UnknownPreservingSemanticModelReader.Read(given.documents.Seal(atHandler), given.documents.Range(23));
    }

    [Fact] void should_preserve_a_later_major_name_as_one_opaque_boundary() => _older.Unknowns.Single().Path.ShouldEqual("/application/eventSources");
    [Fact] void should_not_apply_later_major_routing_security_or_discover_opaque_semantics() => _older.Unknowns.Single().Category.ShouldEqual(UnknownEsmCategory.Unclassified);
    [Fact] void should_attribute_the_opaque_boundary_to_the_known_application() => _older.Unknowns.Single().Owner.ShouldEqual(_older.Model.Application.Id);
    [Fact] void should_preserve_the_entire_opaque_value() => Encoding.UTF8.GetString(_older.Unknowns.Single().Value.AsSpan()).ShouldEqual("{\"a\":[null],\"z\":{\"id\":\"not-an-identity\",\"policies\":true}}");
    [Fact] void should_reconstruct_the_full_wire_bytes_unchanged() => _older.CopyBytes().AsSpan().SequenceEqual(_input).ShouldBeTrue();
    [Fact] void should_keep_later_major_fields_out_of_the_known_projection() => _older.Model.Revision.ShouldEqual(SemanticModelSerializer.Deserialize(given.documents.Frozen(1)).Revision);
    [Fact] void should_still_refuse_unknowns_at_the_registered_pair() => _registeredRefusal.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_apply_the_same_rule_at_a_later_command_boundary() => (_handler.Unknowns.Single().Name == "handler" && _handler.Unknowns.Single().Owner == _command && !_handler.Unknowns.Single().IsSecurityRelevant).ShouldBeTrue();
    [Fact] void should_recognize_that_field_only_when_its_baseline_major_is_reached() => _newer.Unknowns.Single().Path.EndsWith("/handler/future", StringComparison.Ordinal).ShouldBeTrue();
}
#endif
