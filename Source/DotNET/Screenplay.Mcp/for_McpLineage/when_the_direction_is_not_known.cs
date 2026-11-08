// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpLineage;

public class when_the_direction_is_not_known : given.a_lineage_model
{
    JsonElement _result;

    void Establish() => Initialize();

    void Because() => _result = Call("trace-lineage", new { address = "Projects.Registration.RegisterProject.ProjectRegistered.name", direction = "sideways" });

    [Fact] void should_refuse_the_arguments() => _result.GetProperty("error").GetProperty("data").GetProperty("failureKind").GetString().ShouldEqual("InvalidArguments");
    [Fact] void should_name_the_direction() => _result.GetProperty("error").GetProperty("message").GetString()!.ShouldContain("'direction'");
}
