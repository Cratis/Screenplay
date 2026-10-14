// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_ReleaseBaselineCommands;

public class when_checking_a_rehomed_state : given.a_release_workspace
{
    ReleaseBaselineOutcome _check = null!;
    ReleaseBaselineOutcome _admission = null!;
    byte[] _baselineBefore = [];
    byte[] _state = [];

    void Establish()
    {
        AdmitBaseline();
        _baselineBefore = File.ReadAllBytes(BaselinePath);
        var split = Source.IndexOf("module Projects", StringComparison.Ordinal);
        var applicationPath = PortablePlayPath.Parse("application.play");
        var featurePath = PortablePlayPath.Parse("Projects/Registration/Registration.play");
        var rehomed = Original.Rehome([
            WorkspaceDocument.Create(ModelRootDocumentKeys.For(applicationPath), applicationPath, Encoding.UTF8.GetBytes(Source[..split])),
            WorkspaceDocument.Create(ModelRootDocumentKeys.For(featurePath), featurePath, Encoding.UTF8.GetBytes(Source[split..]))
        ]);
        foreach (var document in rehomed.Documents)
        {
            var path = Path.Combine(RootPath, document.Path.Value);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllBytes(path, [.. document.Bytes]);
        }

        _state = PersistedIdentityStateSerializer.Serialize(PersistedIdentityStateSerializer.From(rehomed));
        File.WriteAllBytes(Files.PathFor(McpState.FileName), _state);
    }

    void Because()
    {
        _check = ReleaseBaselineCommands.Check(RootPath);
        _admission = ReleaseBaselineCommands.Admit(RootPath, "v2");
    }

    [Fact] void should_check_cleanly() => _check.ExitCode.ShouldEqual(0);
    [Fact] void should_report_no_violations() => _check.Violations.ShouldBeEmpty();
    [Fact] void should_readmit_successfully() => _admission.ExitCode.ShouldEqual(0);
    [Fact] void should_add_no_entries() => _admission.AdmittedEntryCount.ShouldEqual(1);
    [Fact] void should_preserve_the_original_admission_bytes() => File.ReadAllBytes(BaselinePath).ShouldEqual(_baselineBefore);
    [Fact] void should_leave_the_rehomed_identity_state_unchanged() => Files.Read(McpState.FileName).ShouldEqual(_state);
    [Fact] void should_leave_no_pending_operation() => Files.Read(McpRecoveryJournal.FileName).ShouldBeNull();
}
