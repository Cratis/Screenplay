// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpDurableState;

public class when_rolling_back_swapped_identical_files : for_McpConnection.given.a_connection
{
    McpDiskResult _result = null!;
    McpManagedFiles _files = null!;
    byte[] _state = [];
    string _firstAccess = string.Empty;
    string _secondAccess = string.Empty;
    string _installedFirstAccess = string.Empty;
    string _installedSecondAccess = string.Empty;
    McpProposal _proposal = null!;

    void Establish()
    {
        McpManagedFiles.WritePrivate(Path.Combine(RootPath, "a.play"), []);
        File.WriteAllBytes(Path.Combine(RootPath, "b.play"), []);
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(Path.Combine(RootPath, "b.play"), UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead | UnixFileMode.OtherRead);
        }

        _firstAccess = Access("a.play");
        _secondAccess = Access("b.play");
        var workspace = Workspace();
        var transaction = workspace.Propose(new()
        {
            ExpectedRevision = workspace.Revision,
            ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
            Operations =
            [
                new MoveWorkspaceDocument { Document = workspace.Documents.Single(document => document.Path.Value == "a.play").Id, Path = PortablePlayPath.Parse("b.play") },
                new MoveWorkspaceDocument { Document = workspace.Documents.Single(document => document.Path.Value == "b.play").Id, Path = PortablePlayPath.Parse("a.play") }
            ]
        });
        transaction.Success.ShouldBeTrue();
        _proposal = new(workspace, transaction);
        _files = new(Root);
        _state = McpState.Serialize(workspace);
        McpManagedFiles.WritePrivate(_files.PathFor(McpState.FileName, create: true), _state);
    }

    void Because() => _result = new McpDisk(Root, (source, destination) =>
    {
        if (destination == _files.PathFor(McpState.FileName))
        {
            _installedFirstAccess = Access("a.play");
            _installedSecondAccess = Access("b.play");
            throw new McpFailure("Injected failure after swapping identical source bytes with different access rules.");
        }

        File.Move(source, destination);
    }).Apply(_proposal);

    [Fact] void should_start_with_distinct_access_rules() => (_firstAccess != _secondAccess).ShouldBeTrue();
    [Fact] void should_have_installed_the_second_files_access_at_the_first_path() => _installedFirstAccess.ShouldEqual(_secondAccess);
    [Fact] void should_have_installed_the_first_files_access_at_the_second_path() => _installedSecondAccess.ShouldEqual(_firstAccess);
    [Fact] void should_report_failure_with_verified_rollback() => _result.Status.ShouldEqual("RolledBack");
    [Fact] void should_not_report_success() => _result.Success.ShouldBeFalse();
    [Fact] void should_restore_the_first_paths_original_access() => Access("a.play").ShouldEqual(_firstAccess);
    [Fact] void should_restore_the_second_paths_original_access() => Access("b.play").ShouldEqual(_secondAccess);
    [Fact] void should_preserve_both_empty_files() => File.ReadAllBytes(Path.Combine(RootPath, "a.play")).Concat(File.ReadAllBytes(Path.Combine(RootPath, "b.play"))).ShouldBeEmpty();
    [Fact] void should_restore_original_state() => _files.Read(McpState.FileName).ShouldEqual(_state);
    [Fact] void should_clear_the_marker_only_after_restoring_access() => _files.Read(McpRecoveryJournal.FileName).ShouldBeNull();

    string Access(string path) => McpRecoveryAccess.Capture(path, Path.Combine(RootPath, path)).Rules;
}
