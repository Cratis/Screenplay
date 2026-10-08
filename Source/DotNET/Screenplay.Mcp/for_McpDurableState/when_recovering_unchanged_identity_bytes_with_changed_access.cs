// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpDurableState;

public class when_recovering_unchanged_identity_bytes_with_changed_access : given.a_durable_workspace
{
    string _originalAccess = string.Empty;
    string _changedAccess = string.Empty;
    string _operationId = string.Empty;
    JsonElement _result;

    void Establish()
    {
        if (OperatingSystem.IsWindows()) return;

        var sourcePath = Path.Combine(RootPath, "application.play");
        var identityPath = Files.PathFor(McpState.FileName);
        File.SetUnixFileMode(sourcePath, File.GetUnixFileMode(identityPath) ^ UnixFileMode.GroupRead);
        _originalAccess = Access();
        _operationId = Prepare().Record.OperationId;
        McpFileAccess.Preserve(sourcePath, identityPath);
        _changedAccess = Access();
    }

    void Because() => _result = Result(new McpWorkspaces(Root).Recover(Arguments(new { operationId = _operationId })));

#pragma warning disable CRSPEC0004 // The analyzer does not recognize the platform-specific FactAttribute subclass.
    [UnixFileModeFact] void should_exercise_different_access_on_identical_bytes() => (_changedAccess != _originalAccess).ShouldBeTrue();
    [UnixFileModeFact] void should_restore_original_access_even_without_rewriting_bytes() => Access().ShouldEqual(_originalAccess);
    [UnixFileModeFact] void should_preserve_original_identity_bytes() => Files.Read(McpState.FileName).ShouldEqual(OriginalState);
    [UnixFileModeFact] void should_report_verified_rollback() => _result.GetProperty("status").GetString().ShouldEqual("RolledBack");
    [UnixFileModeFact] void should_clear_the_marker_after_access_verification() => Files.Read(McpRecoveryJournal.FileName).ShouldBeNull();
#pragma warning restore CRSPEC0004

    string Access() => McpRecoveryAccess.Capture(".screenplay/identities.json", Files.PathFor(McpState.FileName)).Rules;
}

internal sealed class UnixFileModeFactAttribute : FactAttribute
{
    public UnixFileModeFactAttribute()
    {
        if (OperatingSystem.IsWindows())
        {
            Skip = "This spec exercises Unix file modes; Windows uses ACLs instead.";
        }
    }
}
