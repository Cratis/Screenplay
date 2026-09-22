// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Tool.Mcp.for_McpDurableState;

public class when_recovering_unchanged_identity_bytes_with_changed_access : given.a_durable_workspace
{
    string _originalAccess = string.Empty;
    string _changedAccess = string.Empty;
    string _operationId = string.Empty;
    JsonElement _result;

    void Establish()
    {
        _originalAccess = Access();
        _operationId = Prepare().Record.OperationId;
        McpFileAccess.Preserve(Path.Combine(RootPath, "application.play"), Files.PathFor(McpState.FileName));
        _changedAccess = Access();
    }

    void Because() => _result = Result(new McpWorkspaces(Root).Recover(Arguments(new { operationId = _operationId })));

    [Fact] void should_exercise_different_access_on_identical_bytes() => (_changedAccess != _originalAccess).ShouldBeTrue();
    [Fact] void should_restore_original_access_even_without_rewriting_bytes() => Access().ShouldEqual(_originalAccess);
    [Fact] void should_preserve_original_identity_bytes() => Files.Read(McpState.FileName).ShouldEqual(OriginalState);
    [Fact] void should_report_verified_rollback() => _result.GetProperty("status").GetString().ShouldEqual("RolledBack");
    [Fact] void should_clear_the_marker_after_access_verification() => Files.Read(McpRecoveryJournal.FileName).ShouldBeNull();

    string Access() => McpRecoveryAccess.Capture(".screenplay/identities.json", Files.PathFor(McpState.FileName)).Rules;
}
