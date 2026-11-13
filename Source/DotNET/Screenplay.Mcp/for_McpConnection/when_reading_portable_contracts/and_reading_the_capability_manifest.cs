// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using Cratis.Screenplay.Agents;

namespace Cratis.Screenplay.Mcp.for_McpConnection.when_reading_portable_contracts;

[PerFactSpecificationLifecycle]
public class and_reading_the_capability_manifest : given.a_connection
{
    const string Agents = "\nagents\n  tokens personal\n  command RegisterProject\n    scope write\n    description \"Register a project\"\n";
    const string View = "capability-manifest";

    void Establish() => Initialize();

    [Fact]
    void should_dispatch_the_view_and_page_the_manifest_bytes()
    {
        Write(Authorized() + Agents);
        var opened = Open();
        var revision = opened.GetProperty("revision").GetString();
        var bytes = new List<byte>();
        string? modelRevision = null;
        var offset = 0;
        while (true)
        {
            var page = modelRevision is null
                ? Read(new { view = View, expectedRevision = revision, offset, limit = 41 })
                : Read(new { view = View, expectedRevision = revision, expectedModelRevision = modelRevision, offset, limit = 41 });
            page.GetProperty("available").GetBoolean().ShouldBeTrue();
            page.GetProperty("format").GetString().ShouldEqual("screenplay-capability-manifest/1");
            modelRevision = page.GetProperty("modelRevision").GetString();
            var payload = page.GetProperty("page");
            bytes.AddRange(payload.GetProperty("bytesBase64").GetBytesFromBase64());
            if (payload.GetProperty("nextOffset").ValueKind == JsonValueKind.Null) break;
            offset = payload.GetProperty("nextOffset").GetInt32();
        }

        using var document = JsonDocument.Parse(bytes.ToArray());
        document.RootElement.GetProperty("operations").GetArrayLength().ShouldEqual(1);
        var path = Path.Combine(RootPath, "application.play");
        var compilation = Workspace().Compilation;
        bytes.ToArray().ShouldEqual(AgentCapabilityManifestJson.Write(AgentCapabilityManifest.From(compilation).Manifest!));
        File.Exists(path).ShouldBeTrue();
        var stale = Call("read-workspace", new { view = View, expectedRevision = revision, expectedModelRevision = "stale", offset = 41, limit = 41 });
        stale.GetProperty("result").GetProperty("isError").GetBoolean().ShouldBeTrue();
    }

    [Theory]
    [InlineData("no-agents", "NoAgentsBlock")]
    [InlineData("error", "CompilationErrors")]
    [InlineData("long", "DescriptionTooLong")]
    void should_refuse_without_any_partial_manifest(string mode, string refusal)
    {
        var source = Authorized();
        if (mode != "no-agents") source += mode == "long" ? Agents.Replace("Register a project", new string('x', 1025), StringComparison.Ordinal) : Agents + "\nunknown root";
        Write(source);
        var workspace = Open();
        var result = Read(new { view = View, expectedRevision = workspace.GetProperty("revision").GetString() });
        result.GetProperty("available").GetBoolean().ShouldBeFalse();
        result.GetProperty("refusal").GetString().ShouldEqual(refusal);
        result.TryGetProperty("page", out _).ShouldBeFalse();
        if (mode == "long") result.GetProperty("operation").GetString().ShouldEqual("Projects.Registration.RegisterProject.RegisterProject");
        var stale = Call("read-workspace", new { view = View, expectedRevision = workspace.GetProperty("revision").GetString(), offset = 1 });
        stale.GetProperty("result").GetProperty("isError").GetBoolean().ShouldBeTrue();
    }

    [Fact]
    void should_make_the_view_discoverable_in_the_schema()
    {
        using var response = JsonDocument.Parse(Connection.Handle("{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"tools/list\"}"));
        var tool = response.RootElement.GetProperty("result").GetProperty("tools").EnumerateArray().Single(tool => tool.GetProperty("name").GetString() == "read-workspace");
        tool.GetProperty("inputSchema").GetRawText().ShouldContain(View);
    }

    void Write(string source) => File.WriteAllText(Path.Combine(RootPath, "application.play"), source, new UTF8Encoding(false));
    static string Authorized() => Source.Replace("module Projects", "policy Staff\n  require role \"Staff\"\nmodule Projects\n  authorize Staff", StringComparison.Ordinal);
    JsonElement Open() => Call("open-workspace", new { applicationName = "Projects" }).GetProperty("result").GetProperty("structuredContent");
    JsonElement Read(object arguments)
    {
        var response = Call("read-workspace", arguments);
        Assert.False(response.TryGetProperty("error", out _), response.GetRawText());
        return response.GetProperty("result").GetProperty("structuredContent");
    }
}
