// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_finding_unresolved_interaction_property_references : given.a_connection
{
    JsonElement _result;

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), """
            module Projects
              feature Registration
                slice StateChange Retry
                  command Retry
                    attemptId String
                slice StateView Lookup
                  readmodel Project
                    id String
                  query Projects => Project
                  screen List
                    data Project via query Projects
                    on click
                      execute Missing
                        with attemptId from item.id
            """);
        Initialize();
    }

    void Because() => _result = Call("find-references", new { address = "Projects.Registration.Retry.Retry.attemptId", kind = "Property" }).GetProperty("result").GetProperty("structuredContent");

    [Fact] void should_not_report_the_property_as_complete() => _result.GetProperty("complete").GetBoolean().ShouldBeFalse();
    [Fact] void should_retain_the_incomplete_mapping_target() => _result.GetProperty("incomplete")[0].GetProperty("reference").GetProperty("role").GetString().ShouldEqual("mapping-target");
    [Fact] void should_not_claim_a_resolved_reader() => _result.GetProperty("references").EnumerateArray().ShouldBeEmpty();
}
