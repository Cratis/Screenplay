// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspacePropertyLinks;

public class when_a_seed_property_is_unresolved : given.a_model
{
    WorkspacePropertyLink _link;

    void Because() => _link = Create(Source + "\nseed\n  for \"project-1\"\n    Registered\n      unknown = \"Ada\"\n").Links.Single(link => link.Reference.Name == "unknown");

    [Fact] void should_keep_the_unresolved_fixture_occurrence() => _link.Reference.Role.ShouldEqual("fixture");
    [Fact] void should_mark_the_source_incomplete() => _link.Reference.IncompleteSourceOwner.ShouldBeTrue();
    [Fact] void should_report_unresolved_evidence() => _link.Resolution.ShouldEqual("incomplete");
    [Fact] void should_not_guess_a_property() => _link.Candidates.ShouldBeEmpty();
    [Fact] void should_keep_the_seed_owner() => _link.Reference.Owner.Kind.ShouldEqual("SeedEvent");
}
