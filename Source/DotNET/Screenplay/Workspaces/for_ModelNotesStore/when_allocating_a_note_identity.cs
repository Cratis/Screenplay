// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_ModelNotesStore;

public class when_allocating_a_note_identity : Specification
{
    string _first = null!;
    string _second = null!;

    void Because()
    {
        _first = ModelNotesStore.NewId();
        _second = ModelNotesStore.NewId();
    }

    [Fact] void should_start_with_the_note_prefix() => _first.ShouldStartWith("note_");
    [Fact] void should_carry_twenty_two_characters_after_the_prefix() => _first["note_".Length..].Length.ShouldEqual(22);
    [Fact] void should_use_only_url_safe_characters() => _first["note_".Length..].All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_').ShouldBeTrue();
    [Fact] void should_be_unique() => _second.ShouldNotEqual(_first);
}
