// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Tool.Mcp.for_McpDurableState;

public class when_querying_an_interrupted_workspace : given.a_durable_workspace
{
    readonly List<JsonElement> _rejections = [];
    JsonElement _status;
    byte[] _marker = [];

    void Establish()
    {
        Initialize();
        Call("describe-application").GetProperty("result").GetProperty("isError").GetBoolean().ShouldBeFalse();
        Interrupt(Prepare(), installState: false);
        _marker = Files.Read(McpRecoveryJournal.FileName)!;
    }

    void Because()
    {
        foreach (var name in new[] { "describe-application", "diagnostics", "search-declarations", "merged-document", "recommend-layout", "find-fixtures", "find-assertion-gaps" })
        {
            _rejections.Add(Call(name).GetProperty("result"));
        }

        _rejections.Add(Call("find-declaration", new { name = "RegisterProject" }).GetProperty("result"));
        _rejections.Add(Call("read-document", new { path = "renamed.play" }).GetProperty("result"));
        foreach (var name in new[] { "declaration-details", "dependencies", "find-references" })
        {
            _rejections.Add(Call(name, new { address = "Projects.Registration.RegisterProject", kind = "Slice" }).GetProperty("result"));
        }

        _status = Call("workspace-state").GetProperty("result");
    }

    [Fact] void should_exercise_all_twelve_source_tools() => _rejections.Count.ShouldEqual(12);
    [Fact] void should_refuse_every_source_read_including_cached_reads() => _rejections.TrueForAll(result => result.GetProperty("isError").GetBoolean()).ShouldBeTrue();
    [Fact] void should_explain_pending_recovery_in_every_rejection() => _rejections.TrueForAll(result => result.GetProperty("structuredContent").GetProperty("message").GetString()!.Contains("PendingOperation", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_allow_recovery_inspection() => _status.GetProperty("isError").GetBoolean().ShouldBeFalse();
    [Fact] void should_report_the_pending_operation() => _status.GetProperty("structuredContent").GetProperty("recovery").GetProperty("pending").GetBoolean().ShouldBeTrue();
    [Fact] void should_preserve_the_marker() => Files.Read(McpRecoveryJournal.FileName).ShouldEqual(_marker);
    [Fact] void should_not_implicitly_undo_the_interrupted_move() => File.Exists(Path.Combine(RootPath, "application.play")).ShouldBeFalse();
}
