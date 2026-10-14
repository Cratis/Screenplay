// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspace.when_rehoming;

public class and_a_document_path_is_a_directory_of_another : given.a_workspace_with_assigned_events
{
    Exception? _sameCase;
    Exception? _otherCase;

    void Because()
    {
        _sameCase = Catch.Exception(() => Original.Rehome([Document("concepts.play", Concepts), Document("concepts.play/events.play", Events)]));
        _otherCase = Catch.Exception(() => Original.Rehome([Document("concepts.play", Concepts), Document("CONCEPTS.play/events.play", Events)]));
    }

    [Fact] void should_refuse_the_same_casing() => _sameCase.ShouldBeOfExactType<InvalidScreenplayWorkspace>();
    [Fact] void should_refuse_a_different_casing() => _otherCase.ShouldBeOfExactType<InvalidScreenplayWorkspace>();
    [Fact] void should_name_the_conflict() => _sameCase!.Message.ShouldContain("is also a directory of another document");
}
