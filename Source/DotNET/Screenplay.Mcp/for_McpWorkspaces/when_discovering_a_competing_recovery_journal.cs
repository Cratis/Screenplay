// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpWorkspaces;

public class when_discovering_a_competing_recovery_journal : given.a_competing_workspace_state
{
    byte[] _marker = [];
    JsonElement _error;
    JsonElement _status;

    void Establish()
    {
        PrepareNestedJournal();
        _marker = NestedFiles.Read(McpRecoveryJournal.FileName)!;
    }

    void Because()
    {
        _error = Call("open-workspace").GetProperty("result");
        _status = Call("workspace-state").GetProperty("result").GetProperty("structuredContent");
    }

    [Fact] void should_block_opening_at_the_outer_root() => _error.GetProperty("structuredContent").GetProperty("failureKind").GetString().ShouldEqual("PendingOperation");
    [Fact] void should_name_the_pending_root_in_the_error() => _error.GetProperty("structuredContent").GetProperty("message").GetString()!.Contains($"'{ModelRoot}'", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_name_the_bound_root_in_structured_data() => _error.GetProperty("structuredContent").GetProperty("rootBindingConflict").GetProperty("boundRoot").GetString().ShouldEqual(RootPath);
    [Fact] void should_disclose_the_pending_root_in_the_error() => _error.GetProperty("structuredContent").GetProperty("rootBindingConflict").GetProperty("pendingRoots").EnumerateArray().Select(value => value.GetString()).ToArray().ShouldEqual([ModelRoot]);
    [Fact] void should_allow_status_inspection_of_the_conflict() => _status.GetProperty("rootBindingConflict").GetProperty("pendingRoots").EnumerateArray().Select(value => value.GetString()).ToArray().ShouldEqual([ModelRoot]);
    [Fact] void should_require_an_explicit_path_for_competing_recovery() => _error.GetProperty("structuredContent").GetProperty("message").GetString()!.Contains("open each competing root with an explicit path", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_preserve_the_competing_journal() => NestedFiles.Read(McpRecoveryJournal.FileName).ShouldEqual(_marker);
    [Fact] void should_leave_original_model_bytes_unchanged() => NestedRoot.Read().Single().Bytes.ToArray().ShouldEqual([.. NestedWorkspace.Documents.Single().Bytes]);
    [Fact] void should_keep_the_journal_at_the_competing_root() => File.Exists(Files.PathFor(McpRecoveryJournal.FileName)).ShouldBeFalse();
}
