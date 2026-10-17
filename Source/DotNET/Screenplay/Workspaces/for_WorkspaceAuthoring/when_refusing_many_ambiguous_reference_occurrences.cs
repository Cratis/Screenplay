// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring;

public class when_refusing_many_ambiguous_reference_occurrences : given.a_reference_batch
{
    const string Consumers = "  feature Consumer0\n    depends on Helper\n  feature Consumer1\n    depends on Helper\n  feature Consumer2\n    depends on Helper\n  feature Consumer3\n    depends on Helper\n  feature Consumer4\n    depends on Helper\n  feature Consumer5\n    depends on Helper\n";
    WorkspaceAuthoringResult _result = null!;

    void Establish() => Configure(("application.play", "module Left\n  feature Helper\n" + Consumers + Right));
    void Because() => _result = Propose(
        Workspace,
        WorkspaceAuthoringReferencePolicy.Draft,
        ("application.play", "module Left\n  feature Helper\n" + Consumers.Replace("    depends on Helper\n", string.Empty, StringComparison.Ordinal) + Right + Consumers));

    [Fact] void should_refuse_the_ambiguous_move() => _result.Accepted.ShouldBeFalse();
    [Fact] void should_bound_the_occurrence_list() => _result.Conflicts.Single().Message.Split("(target, Container)", StringSplitOptions.None).Length.ShouldEqual(11);
    [Fact] void should_disclose_the_omitted_occurrence_count() => _result.Conflicts.Single().Message.ShouldContain("and 2 more");
}
