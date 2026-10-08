// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Completeness.for_NavigationCompleteness;

public class when_a_cycle_has_no_path_from_a_root : given.screens
{
    void Establish() => Compile("screen Home\nscreen One\n  on load\n    navigate to Two\nscreen Two\n  on load\n    navigate to One", "on load\n  navigate to Home");
    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.Navigation]));

    [Fact] void should_report_both_members_of_the_unreachable_cycle() => Findings.Select(finding => finding.Message).ShouldContainOnly("Screen 'One' is unreachable from navigation entry points", "Screen 'Two' is unreachable from navigation entry points");
}
