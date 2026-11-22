// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_reporting_a_large_agent_allowlist : given.a_connection
{
    JsonElement _sourceWarning;
    JsonElement _executableWarning;
    JsonElement _model;
    JsonElement _manifest;
    int _agentsLine;

    void Establish()
    {
        var split = Source.IndexOf("    slice ", StringComparison.Ordinal);
        var header = Source[..split].Replace("module Projects", "policy Staff\n  require role \"Staff\"\nmodule Projects\n  authorize Staff", StringComparison.Ordinal);
        var slices = Enumerable.Range(0, 21).Select(index => Source[split..].Replace("RegisterProject", $"Operation{index}", StringComparison.Ordinal).Replace("ProjectRegistered", $"Operation{index}Registered", StringComparison.Ordinal));
        var operations = Enumerable.Range(0, 21).Select(index => $"  command Operation{index}\n    scope write\n    description \"Register a project\"");
        var model = header + string.Join('\n', slices) + "\nagents\n  tokens personal\n" + string.Join('\n', operations);
        _agentsLine = model.Split('\n').ToList().IndexOf("agents") + 1;
        File.WriteAllText(Path.Combine(RootPath, "application.play"), model);
        Initialize();
    }

    void Because()
    {
        var revision = Content("open-workspace").GetProperty("revision").GetString();
        _sourceWarning = Warning(Content("diagnostics"));
        _executableWarning = Warning(Content("read-workspace", new { view = "executable-diagnostics", expectedRevision = revision }));
        _model = Content("read-workspace", new { view = "executable-model", expectedRevision = revision });
        _manifest = Content("read-workspace", new { view = "capability-manifest", expectedRevision = revision, limit = 16384 });
    }

    [Fact] void should_serialize_warning_severity() => _sourceWarning.GetProperty("severity").GetString().ShouldEqual("Warning");
    [Fact] void should_serialize_the_same_executable_warning() => _executableWarning.GetRawText().ShouldEqual(_sourceWarning.GetRawText());
    [Fact] void should_anchor_the_warning_to_the_agents_block() => _sourceWarning.GetProperty("location").GetProperty("line").GetInt32().ShouldEqual(_agentsLine);
    [Fact] void should_serialize_the_count_argument_kind() => _sourceWarning.GetProperty("arguments").GetProperty("count").GetProperty("kind").GetString().ShouldEqual("text");
    [Fact] void should_serialize_the_count_argument_value() => _sourceWarning.GetProperty("arguments").GetProperty("count").GetProperty("value").GetString().ShouldEqual("21");
    [Fact] void should_leave_executable_export_available() => _model.GetProperty("available").GetBoolean().ShouldBeTrue();
    [Fact] void should_leave_the_manifest_available() => _manifest.GetProperty("available").GetBoolean().ShouldBeTrue();

    [Fact]
    void should_export_every_explicitly_exposed_operation()
    {
        using var manifest = JsonDocument.Parse(_manifest.GetProperty("page").GetProperty("bytesBase64").GetBytesFromBase64());
        manifest.RootElement.GetProperty("operations").GetArrayLength().ShouldEqual(21);
    }

    JsonElement Content(string tool, object? arguments = null) => Call(tool, arguments).GetProperty("result").GetProperty("structuredContent");
    static JsonElement Warning(JsonElement result) => result.GetProperty("page").GetProperty("items").EnumerateArray().Single(item => item.GetProperty("code").GetString() == "PLAY1167");
}
