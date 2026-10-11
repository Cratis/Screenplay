// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_finding_inherited_policy_property_references : given.a_connection
{
    JsonElement _result;

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), """
            policy Owns
              require claim "owner" matches ownerId
            module Projects
              authorize Owns
              feature Registration
                slice StateChange Register
                  command Register
                    ownerId String
            """);
        Initialize();
    }

    void Because() => _result = Call("find-references", new { address = "Projects.Registration.Register.Register.ownerId", kind = "Property" }).GetProperty("result").GetProperty("structuredContent");

    [Fact] void should_report_the_policy_reader() => _result.GetProperty("references")[0].GetProperty("owner").GetProperty("address").GetString().ShouldEqual("Owns");
    [Fact] void should_report_the_condition_role() => _result.GetProperty("references")[0].GetProperty("role").GetString().ShouldEqual("condition");
    [Fact] void should_report_a_complete_link_set() => _result.GetProperty("complete").GetBoolean().ShouldBeTrue();
}
