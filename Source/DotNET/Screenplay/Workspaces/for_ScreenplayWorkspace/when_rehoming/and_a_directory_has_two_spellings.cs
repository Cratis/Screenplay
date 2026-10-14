// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspace.when_rehoming;

public class and_a_directory_has_two_spellings : given.a_workspace_with_assigned_events
{
    Exception? _twoSpellings;
    Exception? _oneSpelling;

    void Because()
    {
        _twoSpellings = Catch.Exception(() => Original.Rehome([Document("Sales/concepts.play", Concepts), Document("sales/events.play", Events)]));
        _oneSpelling = Catch.Exception(() => Original.Rehome([Document("Sales/concepts.play", Concepts), Document("Sales/events.play", Events)]));
    }

    [Fact] void should_refuse_two_spellings_of_one_directory() => _twoSpellings.ShouldBeOfExactType<InvalidScreenplayWorkspace>();
    [Fact] void should_name_both_spellings() => _twoSpellings!.Message.ShouldContain("is also spelled");
    [Fact] void should_accept_one_spelling() => _oneSpelling.ShouldBeNull();
}
