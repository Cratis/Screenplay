// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_reading_event_subjects : given.an_authoring_connection
{
    [Theory]
    [InlineData("")]
    [InlineData(" subject")]
    void should_disclose_the_subject_source_and_property_role(string modifier)
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), $"module Projects\n  feature Naming\n    slice StateChange Rename\n      event Renamed\n        customerId Uuid{modifier}\n");
        Initialize();
        Open();
        var summary = Result("declaration-details", new { address = "Projects.Naming.Rename.Renamed", kind = "Event" }).GetProperty("details").GetProperty("subject");
        summary.GetProperty("source").GetString().ShouldEqual(modifier.Length == 0 ? "eventSource" : "property");
        if (modifier.Length > 0) summary.GetProperty("property").GetString().ShouldEqual("customerId");
        var property = Result("declaration-details", new { address = "Projects.Naming.Rename.Renamed", kind = "Event", view = "properties" }).GetProperty("details").GetProperty("items").EnumerateArray().Single();
        property.GetProperty("isSubject").GetBoolean().ShouldEqual(modifier.Length > 0);
    }
}
