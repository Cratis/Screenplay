// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspace.when_rehoming;

public class and_the_split_drops_a_declaration : given.a_workspace_with_assigned_events
{
    Exception _exception = null!;

    void Because() => _exception = Catch.Exception(() => Original.Rehome([Document("application.play", Concepts), Document("Projects/Registration/Registration.play", Events.Replace("      event ProjectRegistered\n        name ProjectName\n", "      command RegisterProject\n        projectId ProjectId identifier\n", StringComparison.Ordinal))]));

    [Fact] void should_refuse() => _exception.ShouldBeOfExactType<InvalidScreenplayWorkspace>();
    [Fact] void should_name_the_retired_address_argument() => _exception.Message.ShouldContain("retiredAddresses");
    [Fact] void should_name_the_event() => _exception.Message.ShouldContain("ProjectRegistered");
}
