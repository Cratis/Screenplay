// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_reading_the_compiler_source_map : given.a_connection
{
    JsonElement _first;
    JsonElement _second;
    JsonElement _stale;
    JsonElement _schema;
    JsonElement _failed;
    string _revision = null!;

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), """
            module Projects
              description "First"
              feature Registration
            """);
        File.WriteAllText(Path.Combine(RootPath, "warning.play"), """
            module Projects
              description "Second"
            """);
        File.WriteAllText(Path.Combine(RootPath, "slice.play"), """
            module Projects
              feature Registration
                slice StateChange RegisterProject
                  description "Register a project"
                  command RegisterProject
            """);
        Initialize();
    }

    void Because()
    {
        _revision = Content("open-workspace", new { }).GetProperty("revision").GetString()!;
        _first = Content("read-workspace", new { expectedRevision = _revision, view = "source-map", limit = 1 });
        _second = Content("read-workspace", new { expectedRevision = _revision, view = "source-map", offset = 1, limit = 200 });
        using var schema = JsonDocument.Parse(Connection.Handle(/*lang=json,strict*/ """{"jsonrpc":"2.0","id":2,"method":"tools/list"}"""));
        _schema = schema.RootElement.Clone();
        File.WriteAllText(Path.Combine(RootPath, "slice.play"), """
            module Projects
              feature Registration
                slice StateChange RegisterProject
                  description "Register a project"
                  command RegisterProject
                    authorize MissingPolicy
            """);
        _stale = Call("read-workspace", new { expectedRevision = _revision, view = "source-map" });
        _failed = Content("open-workspace", new { });
        _failed = Content("read-workspace", new { expectedRevision = _failed.GetProperty("revision").GetString(), view = "source-map" });
    }

    [Fact] void should_make_the_map_available_for_a_successful_compilation() => _first.GetProperty("available").GetBoolean().ShouldBeTrue();
    [Fact] void should_page_source_map_entries() => _first.GetProperty("page").GetProperty("nextOffset").GetInt32().ShouldEqual(1);
    [Fact] void should_preserve_revision_on_continuation() => _second.GetProperty("page").GetProperty("revision").GetString().ShouldEqual(_revision);
    IEnumerable<JsonElement> Entries() => _first.GetProperty("page").GetProperty("items").EnumerateArray().Concat(_second.GetProperty("page").GetProperty("items").EnumerateArray());
    [Fact] void should_report_the_split_file_declaration_at_the_original_line()
    {
        var entry = Entries().First(item => item.GetProperty("role").GetString() == "Declaration" && item.GetProperty("path").GetString() == "slice.play" && item.GetProperty("span").GetProperty("startLine").GetInt32() == 3);
        entry.GetProperty("span").GetProperty("length").GetInt32().ShouldEqual(0);
        entry.GetProperty("semanticId").GetString().ShouldNotBeEmpty();
    }
    [Fact] void should_report_a_split_file_description_with_an_exact_span()
    {
        var entry = Entries().First(item => item.GetProperty("role").GetString() == "Description");
        entry.GetProperty("path").GetString().ShouldEqual("slice.play");
        entry.GetProperty("semanticId").GetString().ShouldNotBeEmpty();
        entry.GetProperty("origin").GetString().ShouldNotBeEmpty();
        entry.GetProperty("documentId").GetString().ShouldNotBeEmpty();
        entry.GetProperty("span").GetProperty("length").GetInt32().ShouldEqual("Register a project".Length);
        entry.GetProperty("span").GetProperty("startLine").GetInt32().ShouldEqual(4);
    }
    [Fact]
    void should_expose_only_explicit_span_coordinates()
    {
        var span = Entries().First().GetProperty("span");
        string.Join(',', span.EnumerateObject().Select(property => property.Name)).ShouldEqual("start,length,startLine,startColumn,endLine,endColumn");
        (span.GetProperty("start").GetInt32() >= 0).ShouldBeTrue();
        span.GetProperty("startColumn").GetInt32().ShouldBeGreaterThan(0);
        span.GetProperty("endLine").GetInt32().ShouldBeGreaterThan(0);
        span.GetProperty("endColumn").GetInt32().ShouldBeGreaterThan(0);
    }
    [Fact] void should_advertise_the_source_map_view() => _schema.GetProperty("result").GetProperty("tools").EnumerateArray().Single(tool => tool.GetProperty("name").GetString() == "read-workspace").GetProperty("inputSchema").GetProperty("properties").GetProperty("view").GetProperty("enum").EnumerateArray().Any(choice => choice.GetString() == "source-map").ShouldBeTrue();
    [Fact] void should_refuse_stale_revision() => _stale.GetProperty("result").GetProperty("isError").GetBoolean().ShouldBeTrue();
    [Fact] void should_point_to_bounded_executable_diagnostics() => _first.GetProperty("executableDiagnosticsView").GetString().ShouldEqual("executable-diagnostics");
    [Fact] void should_count_warnings_even_when_the_map_is_available() => _first.GetProperty("executableDiagnosticsCount").GetInt32().ShouldBeGreaterThan(0);
    [Fact] void should_not_misrepresent_a_failed_compilation_as_an_empty_successful_map()
    {
        _failed.GetProperty("available").GetBoolean().ShouldBeFalse();
        _failed.GetProperty("executableDiagnosticsCount").GetInt32().ShouldBeGreaterThan(0);
        _failed.GetProperty("page").GetProperty("items").GetArrayLength().ShouldEqual(0);
    }

    JsonElement Content(string tool, object arguments) => Call(tool, arguments).GetProperty("result").GetProperty("structuredContent");
}
