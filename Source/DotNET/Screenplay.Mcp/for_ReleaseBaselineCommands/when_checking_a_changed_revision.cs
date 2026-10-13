// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_ReleaseBaselineCommands;

public class when_checking_a_changed_revision : given.a_release_workspace
{
    ReleaseBaselineOutcome _outcome = null!;
    byte[] _baseline = [];

    void Establish()
    {
        AdmitBaseline();
        _baseline = File.ReadAllBytes(BaselinePath);
        File.WriteAllText(Path.Combine(RootPath, "application.play"), Source.Replace("name ProjectName", "name ProjectName?", StringComparison.Ordinal));
    }

    void Because() => _outcome = ReleaseBaselineCommands.Check(RootPath);

    [Fact] void should_report_a_violation_exit() => _outcome.ExitCode.ShouldEqual(1);
    [Fact] void should_name_the_change() => _outcome.Violations.Single().Kind.ShouldEqual("BaselinedRevisionChanged");
    [Fact] void should_name_the_contract() => _outcome.Violations.Single().Event.ShouldEqual(Original.IdentityCatalog.EventContracts.Single().Id.ToString());
    [Fact] void should_name_the_revision() => _outcome.Violations.Single().Revision.ShouldEqual(1U);
    [Fact] void should_name_the_changed_path() => _outcome.Violations.Single().Path!.ShouldContain("optional");
    [Fact] void should_preserve_the_baseline() => File.ReadAllBytes(BaselinePath).ShouldEqual(_baseline);
    [Fact] void should_preserve_identities() => Files.Read(McpState.FileName).ShouldEqual(OriginalState);
}
