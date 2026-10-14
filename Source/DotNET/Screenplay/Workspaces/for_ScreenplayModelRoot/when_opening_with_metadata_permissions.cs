// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_ScreenplayModelRoot;

public class when_opening_with_metadata_permissions : given.a_persisted_model_root
{
    [Theory]
    [InlineData(493, true)] // 0755: a normal git checkout.
    [InlineData(509, false)] // 0775: writable by the group.
    [InlineData(495, false)] // 0757: writable by others.
    void should_check_read_permissions_without_changing_metadata(int permissions, bool accepted)
    {
        if (OperatingSystem.IsWindows()) return;
        AdmitBaseline();
        var directory = Path.Combine(RootPath, ".screenplay");
        File.SetUnixFileMode(directory, (UnixFileMode)permissions);
        var before = Snapshot();
        var outcomes = new[] { ScreenplayModelRoot.Open(RootPath), ScreenplayModelRoot.OpenForReleaseBaselineCheck(RootPath) };
        foreach (var outcome in outcomes)
        {
            outcome.Success.ShouldEqual(accepted);
            if (accepted)
            {
                outcome.Workspace!.Compilation.Success.ShouldBeTrue();
                CatalogOf(outcome).ShouldEqual(PersistedCatalog);
            }
            else
            {
                outcome.FailureKind.ShouldEqual("MetadataPermissions");
                outcome.Reason!.ShouldContain(".screenplay must not be writable by other users");
            }
        }

        Snapshot().ShouldEqual(before);
        File.GetUnixFileMode(directory).ShouldEqual((UnixFileMode)permissions);
    }
}
