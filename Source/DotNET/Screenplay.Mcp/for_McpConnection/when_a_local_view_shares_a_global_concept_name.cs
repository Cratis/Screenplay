// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_a_local_view_shares_a_global_concept_name : given.a_connection
{
    JsonElement _references;

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), """
            concept Status : String
            module Billing
              feature Orders
                slice StateView Current
                  readmodel Status
                    value String
                slice StateChange Change
                  command ChangeStatus
                    status Status
            """);
        Initialize();
    }

    void Because() => _references = Call("find-references", new { address = "Status", kind = "Concept" }).GetProperty("result").GetProperty("structuredContent");

    [Fact] void should_compile_without_errors() => _references.GetProperty("success").GetBoolean().ShouldBeTrue();
    [Fact] void should_resolve_the_property_type_to_the_global_concept() => _references.GetProperty("references").GetArrayLength().ShouldEqual(1);
    [Fact] void should_identify_the_command_as_owner() => _references.GetProperty("references")[0].GetProperty("owner").GetProperty("name").GetString().ShouldEqual("ChangeStatus");
}
