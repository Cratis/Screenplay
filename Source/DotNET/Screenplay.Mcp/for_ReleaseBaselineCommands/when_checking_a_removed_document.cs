// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_ReleaseBaselineCommands;

public class when_checking_a_removed_document : given.a_release_workspace
{
    ReleaseBaselineOutcome _outcome = null!;

    void Establish()
    {
        AdmitBaseline();
        File.Delete(Path.Combine(RootPath, "application.play"));
    }

    void Because() => _outcome = ReleaseBaselineCommands.Check(RootPath);

    [Fact] void should_report_a_violation_exit() => _outcome.ExitCode.ShouldEqual(1);
    [Fact] void should_retain_the_removed_contract_identity() => _outcome.Violations.Single().Event.ShouldEqual(Original.IdentityCatalog.EventContracts.Single().Id.ToString());
    [Fact] void should_report_a_missing_revision() => _outcome.Violations.Single().Kind.ShouldEqual("BaselinedRevisionMissing");
    [Fact] void should_not_reconcile_identities() => Files.Read(McpState.FileName).ShouldEqual(OriginalState);
    [Fact] void should_not_restore_source() => File.Exists(Path.Combine(RootPath, "application.play")).ShouldBeFalse();
}
