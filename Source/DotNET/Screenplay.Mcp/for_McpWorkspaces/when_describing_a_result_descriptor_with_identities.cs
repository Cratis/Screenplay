// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Mcp.for_McpWorkspaces;

public class when_describing_a_result_descriptor_with_identities : Specification
{
    SemanticId _eventContract;
    SemanticId _shape;
    SemanticId _property;
    SemanticId _target;
    SemanticId _destinationShape;
    JsonNode _entry = null!;

    void Establish()
    {
        _eventContract = SemanticId.Create(SemanticKind.Slice, "event-contract");
        _shape = SemanticId.Create(SemanticKind.Slice, "payload-shape");
        _property = SemanticId.Create(SemanticKind.Slice, "payload-property");
        _target = SemanticId.Create(SemanticKind.Slice, "model-target");
        _destinationShape = SemanticId.Create(SemanticKind.Slice, "destination-shape");
    }

    void Because()
    {
        var payload = new SemanticContextType(SemanticContextTypeKinds.Shape, null, _shape, null)
        {
            Properties = [new("Amount", _property, new(SemanticTypeReferenceKind.Concept, default, _target, false, false))]
        };
        var entry = new SemanticHandlerResultEntry(_eventContract, "Recorded", 1, payload, SemanticHandlerResultDestinationKinds.Scalar)
        {
            DestinationProperty = "Id",
            DestinationType = new(SemanticContextTypeKinds.Shape, null, _destinationShape, null)
        };
        var descriptor = new SemanticTypedContextDescriptor("handler", SemanticImplementationRole.CommandHandler, 1, null, []) { Result = new(2, [entry]) };
        var node = JsonSerializer.SerializeToNode(McpWorkspaces.DescribeDescriptor(descriptor), McpJson.Options);
        _entry = node["result"]["entries"][0];
    }

    [Fact] void should_write_the_event_contract_as_its_identity() => _entry["eventContract"].GetValue<string>().ShouldEqual(_eventContract.ToString());
    [Fact] void should_write_the_payload_shape_as_its_identity() => _entry["payload"]["shape"].GetValue<string>().ShouldEqual(_shape.ToString());
    [Fact] void should_write_the_property_identity() => _entry["payload"]["properties"][0]["id"].GetValue<string>().ShouldEqual(_property.ToString());
    [Fact] void should_write_the_model_target_as_its_identity() => _entry["payload"]["properties"][0]["type"]["target"].GetValue<string>().ShouldEqual(_target.ToString());
    [Fact] void should_write_the_destination_shape_as_its_identity() => _entry["destinationType"]["shape"].GetValue<string>().ShouldEqual(_destinationShape.ToString());
    [Fact] void should_keep_the_destination_property() => _entry["destinationProperty"].GetValue<string>().ShouldEqual("Id");
}
