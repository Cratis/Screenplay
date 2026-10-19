// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceFixtureCandidates;

public class when_computing_the_candidates : for_WorkspaceFixtureDeclarations.given.a_fixture_model
{
    WorkspaceFixtureCandidates _first = null!;
    WorkspaceFixtureCandidates _second = null!;

    void Because()
    {
        _first = WorkspaceFixtureCandidates.For(Index);
        _second = WorkspaceFixtureCandidates.For(Index);
    }

    [Fact] void should_compute_the_candidates_once_per_index() => _second.ShouldBeSame(_first);
    [Fact] void should_keep_the_latest_generation_per_scope() => Generations("ProjectRegistered").ShouldContainOnly(1u, 2u);
    [Fact] void should_keep_only_the_latest_generation_of_another_scope() => Generations("ProjectArchived").ShouldContainOnly(3u);
    [Fact] void should_keep_one_candidate_per_scope() => _first.Entries.Count(entry => entry.Declaration.Name == "ProjectRegistered").ShouldEqual(2);
    [Fact] void should_find_the_entry_of_each_declaration_as_a_scan_does() => _first.Entries.Select(candidate => _first.EntryOf(candidate.Declaration)).ShouldEqual(_first.Entries.Select(Scanned));
    [Fact] void should_find_no_entry_for_a_declaration_it_does_not_hold() => _first.EntryOf(new("ProjectRegistered", new(["Projects", "Registration", "RegisterProject"]))).ShouldBeNull();
    [Fact] void should_find_no_entry_without_a_declaration() => _first.EntryOf(null).ShouldBeNull();

    WorkspaceSyntaxEntry? Scanned((WorkspaceSyntaxEntry Entry, Parsing.Declaration Declaration) candidate) =>
        _first.Entries.Where(entry => entry.Declaration == candidate.Declaration).Take(2).ToArray() is [var match] ? match.Entry : null;

    IEnumerable<uint> Generations(string name) => _first.Entries.Select(entry => entry.Entry.Node).OfType<EventSyntax>().Where(@event => @event.Name == name).Select(@event => @event.Generation);
}
