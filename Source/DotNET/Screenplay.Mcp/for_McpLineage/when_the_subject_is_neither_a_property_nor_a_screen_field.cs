// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpLineage;

public class when_the_subject_is_not_a_read_model_property : given.a_lineage_model
{
    JsonElement _result;

    void Establish() => Initialize();

    void Because() => _result = Call("trace-lineage", new { address = "Projects.Registration.RegisterProject.ProjectRegistered.name" }).GetProperty("result");

    [Fact] void should_refuse_the_trace() => _result.GetProperty("isError").GetBoolean().ShouldBeTrue();
    [Fact] void should_say_a_read_model_property_is_required() => _result.GetProperty("content")[0].GetProperty("text").GetString()!.ShouldContain("read-model property");
}
