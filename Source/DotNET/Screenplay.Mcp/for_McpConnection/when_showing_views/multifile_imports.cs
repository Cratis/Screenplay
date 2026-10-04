// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Mcp.for_McpConnection.when_showing_views;

public class multifile_imports : given.a_multifile_model
{
    [Theory]
    [InlineData("application.play")]
    [InlineData("Projects/module.play")]
    [InlineData("Projects/Registration/feature.play")]
    [InlineData("Projects/Registration/barrel.play")]
    [InlineData(SlicePath)]
    public void should_sketch_each_level_against_the_whole_application(string path)
    {
        var source = File.ReadAllText(Path.Combine(RootPath, path)) + "\n// sketched\n";
        var result = Call("visualize-model", new { sketch = new[] { new { path, source } } }).GetProperty("result");
        result.GetProperty("isError").GetBoolean().ShouldBeFalse();
        var content = result.GetProperty("structuredContent");
        content.GetProperty("documents").GetArrayLength().ShouldEqual(5);
        foreach (var state in new[] { "current", "proposed" })
        {
            content.GetProperty(state).GetProperty("slices").GetInt32().ShouldEqual(1);
            content.GetProperty(state).GetProperty("events").GetInt32().ShouldEqual(1);
            content.GetProperty(state).GetProperty("errors").GetInt32().ShouldEqual(0);
        }
        content.GetProperty("added").GetArrayLength().ShouldEqual(0);
        content.GetProperty("removed").GetArrayLength().ShouldEqual(0);
        content.GetProperty("changes")[0].GetProperty("path").GetString().ShouldEqual(path);
    }

    [Fact]
    public void should_add_a_nested_document_and_follow_its_import()
    {
        const string path = "Projects/Registration/RenameProject.play";
        var feature = File.ReadAllText(Path.Combine(RootPath, "Projects/Registration/feature.play"));
        var result = Call("visualize-model", new { sketch = new[]
        {
            new { path = "Projects/Registration/feature.play", source = feature + "\n  import \"RenameProject.play\"" },
            new { path, source = Slice.Replace("RegisterProject", "RenameProject", StringComparison.Ordinal).Replace("ProjectRegistered", "ProjectRenamed", StringComparison.Ordinal) }
        } }).GetProperty("result");
        result.GetProperty("isError").GetBoolean().ShouldBeFalse();
        var content = result.GetProperty("structuredContent");
        content.GetProperty("proposed").GetProperty("slices").GetInt32().ShouldEqual(2);
        content.GetProperty("proposed").GetProperty("errors").GetInt32().ShouldEqual(0);
        content.GetProperty("added").EnumerateArray().Select(item => item.GetString()).ShouldContain("Slice Projects.Registration.RenameProject");
        File.Exists(Path.Combine(RootPath, path)).ShouldBeFalse();
    }

    [Fact]
    public void should_report_missing_imports_instead_of_verifying_partial_assembly()
    {
        var result = Call("visualize-model", new { sketch = new[] { new { path = "Projects/module.play", source = "module Projects\n  import \"missing.play\"" } } }).GetProperty("result");
        result.GetProperty("isError").GetBoolean().ShouldBeFalse();
        result.GetProperty("structuredContent").GetProperty("proposed").GetProperty("errors").GetInt32().ShouldBeGreaterThan(0);
    }

    [Fact]
    public void should_not_index_a_barrel_with_ambiguous_placement_as_a_verified_slice()
    {
        Write("Projects/module.play", "module Projects\n  import \"Registration/feature.play\"\n  feature Other\n    import \"Registration/barrel.play\"");
        var result = Call("visualize-model").GetProperty("result").GetProperty("structuredContent");
        result.GetProperty("current").GetProperty("errors").GetInt32().ShouldBeGreaterThan(0);
        result.GetProperty("current").GetProperty("slices").GetInt32().ShouldEqual(0);
        new McpSnapshot(Root.Read()).Index.Declarations.Where(declaration => declaration.Kind == "Command").ShouldBeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_draw_the_whole_model_with_a_static_or_explicit_dynamic_root(bool dynamic)
    {
        if (dynamic)
        {
            Connection = new(new McpTools(), new McpAppResources(BoardHtml));
            Handle(/*lang=json,strict*/ """{"jsonrpc":"2.0","id":0,"method":"initialize","params":{"protocolVersion":"2025-06-18","capabilities":{"extensions":{"io.modelcontextprotocol/ui":{"mimeTypes":["text/html;profile=mcp-app"]}}},"clientInfo":{"name":"host","version":"1"}}}""");
            Connection.Handle(/*lang=json,strict*/ """{"jsonrpc":"2.0","method":"notifications/initialized"}""");
            Call("open-workspace", new { path = RootPath }).GetProperty("result").GetProperty("isError").GetBoolean().ShouldBeFalse();
        }

        var result = Call("visualize-model", new { sketch = new[] { new { path = SlicePath, source = Slice + "\n// sketch" } } }).GetProperty("result");
        result.GetProperty("isError").GetBoolean().ShouldBeFalse();
        var content = result.GetProperty("structuredContent");
        content.GetProperty("current").GetProperty("slices").GetInt32().ShouldEqual(1);
        content.GetProperty("proposed").GetProperty("slices").GetInt32().ShouldEqual(1);
        content.GetProperty("current").GetProperty("errors").GetInt32().ShouldEqual(0);
        content.GetProperty("proposed").GetProperty("errors").GetInt32().ShouldEqual(0);
    }

    [Fact]
    public void should_read_fresh_disk_source_for_each_sketch_request()
    {
        Call("visualize-model").GetProperty("result").GetProperty("isError").GetBoolean().ShouldBeFalse();
        Write(SlicePath, Slice.Replace("RegisterProject", "CreateProject", StringComparison.Ordinal));
        var result = Call("visualize-model", new { sketch = new[] { new { path = "application.play", source = "import \"Projects/module.play\"\n// sketch" } } }).GetProperty("result").GetProperty("structuredContent");
        result.GetProperty("documents").EnumerateArray().Single(document => document.GetProperty("path").GetString() == SlicePath)
            .GetProperty("source").GetString().ShouldContain("CreateProject");
        result.GetProperty("proposed").GetProperty("errors").GetInt32().ShouldEqual(0);
        result.GetProperty("added").GetArrayLength().ShouldEqual(0);
        result.GetProperty("removed").GetArrayLength().ShouldEqual(0);
    }

    [Fact]
    public void should_keep_physical_source_identity_and_assembled_scope()
    {
        var snapshot = new McpSnapshot(Root.Read());
        snapshot.Compilation.Success.ShouldBeTrue();
        var slice = snapshot.Index.Declarations.Single(declaration => declaration.Kind == "Slice");
        slice.Address.ShouldEqual("Projects.Registration.RegisterProject");
        slice.Location.Path.ShouldEqual(SlicePath);
        var syntax = (SliceSyntax)slice.Syntax;
        syntax.Commands.Single().Location.Path.ShouldEqual(SlicePath);
        snapshot.ParsedDocumentCount.ShouldEqual(5);
    }
}
