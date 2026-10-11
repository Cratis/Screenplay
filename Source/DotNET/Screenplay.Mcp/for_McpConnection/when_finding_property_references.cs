// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_finding_property_references : given.a_connection
{
    JsonElement _result;

    void Establish() => Initialize();
    void Because() => _result = Call("find-references", new { address = "Projects.Registration.RegisterProject.ProjectRegistered.name", kind = "Property" }).GetProperty("result").GetProperty("structuredContent");

    [Fact] void should_find_the_property_target() => _result.GetProperty("declaration").GetProperty("kind").GetString().ShouldEqual("Property");
    [Fact] void should_report_the_mapping_role() => _result.GetProperty("references")[0].GetProperty("role").GetString().ShouldEqual("mapping-target");
    [Fact] void should_report_the_other_end() => _result.GetProperty("references")[0].GetProperty("otherEnd").GetProperty("address").GetString().ShouldEqual("Projects.Registration.RegisterProject.RegisterProject.name");
    [Fact] void should_report_the_unclassified_occurrence_as_incomplete() => _result.GetProperty("complete").GetBoolean().ShouldBeFalse();
    [Fact] void should_retain_the_same_named_field_in_the_referencing_command() => _result.GetProperty("incomplete")[0].GetProperty("reference").GetProperty("role").GetString().ShouldEqual("unclassified");
}
