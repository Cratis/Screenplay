// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_querying_denial_and_appended_fixtures : given.a_connection
{
    const string Address = "Projects.Registration.RegisterProject";
    JsonElement _gaps;
    JsonElement _payload;
    JsonElement _destination;
    JsonElement _references;
    JsonElement _dependencies;
    JsonElement _details;
    JsonElement _description;

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), """
            concept ProjectId : Uuid
            module Projects
              feature Registration
                slice StateChange RegisterProject
                  command RegisterProject
                    projectId ProjectId identifier
                    produces ProjectRegistered
                      for projectId
                      projectId = projectId
                  event ProjectRegistered
                    projectId ProjectId
                  specification Denied
                    when RegisterProject
                    then denied
                  specification Appended
                    when append ProjectRegistered
                      for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                      projectId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                    then ProjectRegistered
                      projectId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                slice StateChange Empty
                  command Nothing
            """);
        Initialize();
    }

    void Because()
    {
        _gaps = Content("find-assertion-gaps", new { });
        _payload = Content("find-fixtures", new { role = "whenAppendedEvent", property = "projectId" });
        _destination = Content("find-fixtures", new { role = "whenAppendedEventDestination", property = "for" });
        _references = Content("find-references", new { address = Address + ".ProjectRegistered", kind = "Event" });
        _dependencies = Content("dependencies", new { address = Address + ".Appended", kind = "Specification", direction = "outgoing" });
        _details = Content("declaration-details", new { address = Address, kind = "Slice", view = "specifications" });
        _description = Content("find-declaration", new { name = "Denied", includeContent = true });
    }

    [Fact] void should_leave_only_the_unasserted_control_as_a_gap() => _gaps.GetProperty("page").GetProperty("matched").GetInt32().ShouldEqual(1);
    [Fact] void should_find_the_appended_payload() => _payload.GetProperty("page").GetProperty("items")[0].GetProperty("role").GetString().ShouldEqual("whenAppendedEvent");
    [Fact] void should_find_the_appended_destination() => _destination.GetProperty("page").GetProperty("items")[0].GetProperty("value").GetString().ShouldEqual("3fa85f64-5717-4562-b3fc-2c963f66afa6");
    [Fact] void should_label_the_appended_reference() => _references.GetProperty("references").EnumerateArray().Any(item => item.GetProperty("role").GetString() == "whenAppendedEvent").ShouldBeTrue();
    [Fact] void should_label_the_appended_dependency() => _dependencies.GetProperty("page").GetProperty("items").EnumerateArray().Any(item => item.ToString().Contains("whenAppendedEvent", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_keep_the_command_control() => _details.GetProperty("details").GetProperty("items")[0].GetProperty("command").GetString().ShouldEqual("RegisterProject");
    [Fact]
    void should_report_denial_without_an_error()
    {
        var denied = _details.GetProperty("details").GetProperty("items")[0];
        denied.GetProperty("thenDenied").GetBoolean().ShouldBeTrue();
        denied.GetProperty("thenErrors").GetInt32().ShouldEqual(0);
    }

    [Fact]
    void should_report_append_without_a_command()
    {
        var appended = _details.GetProperty("details").GetProperty("items")[1];
        appended.GetProperty("command").ValueKind.ShouldEqual(JsonValueKind.Null);
        appended.GetProperty("whenAppendedEvent").GetString().ShouldEqual("ProjectRegistered");
    }
    [Fact] void should_show_denial_in_compact_declaration() => _description.GetProperty("matches")[0].GetProperty("details").GetProperty("thenDenied").GetBoolean().ShouldBeTrue();

    JsonElement Content(string tool, object arguments) => Call(tool, arguments).GetProperty("result").GetProperty("structuredContent");
}
