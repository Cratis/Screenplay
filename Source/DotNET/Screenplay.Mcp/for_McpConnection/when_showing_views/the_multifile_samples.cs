// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Files;

namespace Cratis.Screenplay.Mcp.for_McpConnection.when_showing_views;

public class the_multifile_samples : given.a_host_that_renders_views
{
    [Theory]
    [InlineData("Commerce", "application.play")]
    [InlineData("Commerce", "Catalog/Catalog.play")]
    [InlineData("Commerce", "Ordering/Orders/Orders.play")]
    [InlineData("Commerce", "Ordering/Orders/PlaceOrder.play")]
    [InlineData("TimeTracking", "timetracking.play")]
    [InlineData("TimeTracking", "Timesheets/Recording/RecordingHours.play")]
    public void should_draw_the_unchanged_sample_and_an_overlay_at_each_path(string sample, string path)
    {
        var repository = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (repository is not null && !Directory.Exists(Path.Combine(repository.FullName, "Samples")))
        {
            repository = repository.Parent;
        }

        var folder = Path.Combine(repository!.FullName, "Samples", sample);
        File.Delete(Path.Combine(RootPath, "application.play"));
        var files = Directory.GetFiles(folder, "*.play", SearchOption.AllDirectories);
        files.ShouldNotBeEmpty();
        foreach (var file in files)
        {
            var target = Path.Combine(RootPath, Path.GetRelativePath(folder, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target);
        }

        var native = new PlayFileCompiler().CompileFolder(folder).Result;
        native.Success.ShouldBeTrue();
        var expected = new McpSnapshot(Root.Read());
        expected.Compilation.Success.ShouldBeTrue();
        var expectedSlices = expected.Index.Declarations.Count(declaration => declaration.Kind == "Slice");
        expectedSlices.ShouldBeGreaterThan(0);
        var source = File.ReadAllText(Path.Combine(folder, path));
        var result = Call("visualize-model", new { sketch = new[] { new { path, source = source + "\n// sketch\n" } } }).GetProperty("result");
        result.GetProperty("isError").GetBoolean().ShouldBeFalse();
        var content = result.GetProperty("structuredContent");
        content.GetProperty("documents").GetArrayLength().ShouldEqual(files.Length);
        content.GetProperty("current").GetProperty("slices").GetInt32().ShouldEqual(expectedSlices);
        content.GetProperty("proposed").GetProperty("slices").GetInt32().ShouldEqual(expectedSlices);
        content.GetProperty("current").GetProperty("errors").GetInt32().ShouldEqual(0);
        content.GetProperty("proposed").GetProperty("errors").GetInt32().ShouldEqual(0);
        content.GetProperty("added").GetArrayLength().ShouldEqual(0);
        content.GetProperty("removed").GetArrayLength().ShouldEqual(0);
        File.ReadAllText(Path.Combine(RootPath, path)).ShouldEqual(source);
    }
}
