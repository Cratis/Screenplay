// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection.when_showing_views;

public class a_multifile_sketch : given.a_multifile_model
{
    JsonElement _sliceResult;
    JsonElement _rootResult;

    void Because()
    {
        _sliceResult = Call("visualize-model", new { sketch = new[] { new { path = SlicePath, source = Slice.Replace("ProjectRegistered", "ProjectCreated", StringComparison.Ordinal) } } }).GetProperty("result");
        _rootResult = Call("visualize-model", new { sketch = new[] { new { path = "application.play", source = "description \"Sketched application\"\nimport \"Projects/module.play\"" } } }).GetProperty("result");
    }

    [Fact] void should_draw_a_nested_slice_sketch() => _sliceResult.GetProperty("isError").GetBoolean().ShouldBeFalse();
    [Fact] void should_draw_a_root_sketch() => _rootResult.GetProperty("isError").GetBoolean().ShouldBeFalse();
    [Fact] void should_keep_the_assembled_slice_scope() => _rootResult.GetProperty("structuredContent").GetProperty("current").GetProperty("slices").GetInt32().ShouldEqual(1);
    [Fact] void should_find_the_sketched_event_at_its_assembled_address() => _sliceResult.GetProperty("structuredContent").GetProperty("added").EnumerateArray().Select(item => item.GetString()).ShouldContain("Event Projects.Registration.RegisterProject.ProjectCreated");
    [Fact] void should_have_no_current_import_errors() => _rootResult.GetProperty("structuredContent").GetProperty("current").GetProperty("errors").GetInt32().ShouldEqual(0);
    [Fact] void should_have_no_sketched_import_errors() => _sliceResult.GetProperty("structuredContent").GetProperty("proposed").GetProperty("errors").GetInt32().ShouldEqual(0);
    [Fact] void should_not_write_the_slice_sketch() => File.ReadAllText(Path.Combine(RootPath, SlicePath)).ShouldEqual(Slice);
}
