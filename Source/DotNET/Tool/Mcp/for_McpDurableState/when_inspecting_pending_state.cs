// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Tool.Mcp.for_McpDurableState;

public class when_inspecting_pending_state : given.a_durable_workspace
{
    JsonElement _status;
    Dictionary<string, byte[]> _before = [];

    void Establish()
    {
        Interrupt(Prepare(), installState: true);
        _before = Directory.EnumerateFiles(RootPath, "*", SearchOption.AllDirectories).ToDictionary(path => path, File.ReadAllBytes);
    }

    void Because() => _status = Result(new McpWorkspaces(Root).State(McpJson.Empty));

    [Fact] void should_report_pending_recovery() => _status.GetProperty("recovery").GetProperty("pending").GetBoolean().ShouldBeTrue();
    [Fact] void should_report_that_rollback_is_available() => _status.GetProperty("recovery").GetProperty("canRollback").GetBoolean().ShouldBeTrue();
    [Fact] void should_not_add_or_remove_files() => Directory.EnumerateFiles(RootPath, "*", SearchOption.AllDirectories).Count().ShouldEqual(_before.Count);
    [Fact] void should_not_modify_any_file() => _before.All(pair => File.ReadAllBytes(pair.Key).AsSpan().SequenceEqual(pair.Value)).ShouldBeTrue();
}
