// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspace.when_rehoming;

public class and_the_split_adds_a_declaration : given.a_workspace_with_assigned_events
{
    Exception _exception = null!;

    void Because() => _exception = Catch.Exception(() => Original.Rehome([Split[0], Document("Projects/Registration/Registration.play", Events + "      event ProjectDeleted\n        name ProjectName\n")]));

    [Fact] void should_refuse() => _exception.ShouldBeOfExactType<InvalidScreenplayWorkspace>();
    [Fact] void should_name_the_added_address_argument() => _exception.Message.ShouldContain("addedAddresses");
    [Fact] void should_name_the_event() => _exception.Message.ShouldContain("ProjectDeleted");
}
