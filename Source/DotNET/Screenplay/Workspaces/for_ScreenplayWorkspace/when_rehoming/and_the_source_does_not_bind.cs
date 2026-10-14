// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspace.when_rehoming;

public class and_the_source_does_not_bind : given.a_workspace_with_assigned_events
{
    Exception _exception = null!;

    void Because() => _exception = Catch.Exception(() => Original.Rehome([Split[0], Document("Projects/Registration/Registration.play", Events.Replace("name ProjectName", "name MissingType", StringComparison.Ordinal))]));

    [Fact] void should_refuse() => _exception.ShouldBeOfExactType<InvalidScreenplayWorkspace>();
    [Fact] void should_include_the_binding_diagnostic() => _exception.Message.ShouldContain("MissingType");
}
