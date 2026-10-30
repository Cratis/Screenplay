// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Mcp.for_McpWorkspaces;

public class when_describing_a_result_descriptor : Specification
{
    SemanticTypedContextDescriptor _legacy = null!;
    JsonObject _node = null!;

    void Establish() => _legacy = new("handler", SemanticImplementationRole.CommandHandler, 1, null, []);

    void Because()
    {
        var descriptor = _legacy with { Result = new(2, [new(default, "Recorded", 1, new(SemanticContextTypeKinds.Shape, null, null, null), SemanticHandlerResultDestinationKinds.Default)]) };
        _node = (JsonObject)McpWorkspaces.DescribeDescriptor(descriptor);
    }

    [Fact] void should_advertise_the_result_version() => _node["resultVersion"].GetValue<uint>().ShouldEqual(2u);
    [Fact] void should_include_the_result_entries() => _node["result"]["entries"].AsArray().Count.ShouldEqual(1);

    [Fact]
    void should_preserve_the_v1_shape()
    {
        var legacy = (JsonObject)_node.DeepClone();
        legacy.Remove("resultVersion");
        legacy.Remove("result");
        legacy.ToJsonString(McpJson.Options).ShouldEqual(JsonSerializer.Serialize(McpWorkspaces.DescribeDescriptor(_legacy), McpJson.Options));
    }
}
