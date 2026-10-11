// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_finding_interaction_property_references : given.a_connection
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
                    status String
                  query Projects => Project
                  screen List
                    data Project via query Projects
                    on click
                      execute Retry
                        with attemptId from item.id
                    action "Again"
                      when item.status == "failed" execute Retry
                        with attemptId from item.id
            """);
        Initialize();
    }

    void Because() => _result = Call("find-references", new { address = "Projects.Registration.Retry.Retry.attemptId", kind = "Property" }).GetProperty("result").GetProperty("structuredContent");

    [Fact] void should_report_both_argument_references() => _result.GetProperty("references").GetArrayLength().ShouldEqual(2);
    [Fact] void should_classify_both_as_mapping_targets() => _result.GetProperty("references").EnumerateArray().All(reference => reference.GetProperty("role").GetString() == "mapping-target").ShouldBeTrue();
    [Fact] void should_report_the_read_model_source() => _result.GetProperty("references").EnumerateArray().All(reference => reference.GetProperty("otherEnd").GetProperty("address").GetString() == "Projects.Registration.Lookup.Project.id").ShouldBeTrue();
    [Fact] void should_report_a_complete_property() => _result.GetProperty("complete").GetBoolean().ShouldBeTrue();
    [Fact] void should_not_hide_any_unclassified_readers() => _result.GetProperty("incomplete").EnumerateArray().ShouldBeEmpty();
}
