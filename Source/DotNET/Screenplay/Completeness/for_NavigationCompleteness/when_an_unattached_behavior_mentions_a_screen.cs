// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Completeness.for_NavigationCompleteness;

public class when_an_unattached_behavior_mentions_a_screen : given.screens
{
    void Establish() => Compile("screen Home", root: "behavior Reusable\n  on load\n    navigate to Home\n");
    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.Navigation]));

    [Fact] void should_not_invent_an_entry_point() => Findings.Single().Message.ShouldContain("no navigation entry points");
}
