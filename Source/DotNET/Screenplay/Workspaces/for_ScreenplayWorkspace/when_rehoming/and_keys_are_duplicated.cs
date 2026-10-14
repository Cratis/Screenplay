// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspace.when_rehoming;

public class and_keys_are_duplicated : given.a_workspace_with_assigned_events
{
    Exception _exception = null!;

    void Because() => _exception = Catch.Exception(() => Original.Rehome([Document("same", "application.play", Concepts), Document("same", "events.play", Events)]));

    [Fact] void should_refuse() => _exception.ShouldBeOfExactType<InvalidScreenplayWorkspace>();
    [Fact] void should_describe_the_duplicate() => _exception.Message.ShouldContain("duplicated");
}
