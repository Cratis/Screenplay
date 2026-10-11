// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_following_outgoing_property_dependencies : given.a_connection
{
    JsonElement _result;

    void Establish() => Initialize();
    void Because() => _result = Call("dependencies", new { address = "Projects.Registration.RegisterProject.RegisterProject.name", kind = "Property", direction = "outgoing" }).GetProperty("result").GetProperty("structuredContent");

    [Fact] void should_follow_the_mapping_to_the_event() => _result.GetProperty("page").GetProperty("items")[0].GetProperty("targets")[0].GetProperty("address").GetString().ShouldEqual("Projects.Registration.RegisterProject.ProjectRegistered.name");
    [Fact] void should_report_resolved_evidence() => _result.GetProperty("page").GetProperty("items")[0].GetProperty("resolution").GetString().ShouldEqual("resolved");
}
