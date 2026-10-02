// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring.when_repairing_command_productions;

public class and_the_type_is_uncertain : given.a_command_production
{
    [Theory]
    [InlineData("$context.identity.id")]
    [InlineData("$env.SERVICE_NAME")]
    [InlineData("\"literal\"")]
    [InlineData("42")]
    [InlineData("unknown.name")]
    [InlineData("details.name.length")]
    [InlineData("details.name + 1")]
    [InlineData("Existing.name")]
    void should_not_guess_a_type(string expression)
    {
        Create(Source.Replace("name = name", $"name = {expression}", StringComparison.Ordinal));
        HasRepair(DiagnosticCodes.UnknownEvent).ShouldBeFalse();
    }

    [Fact]
    void should_not_traverse_an_optional_parent()
    {
        Create(Source.Replace("details Details", "details Details?", StringComparison.Ordinal).Replace("name = name", "name = details.name", StringComparison.Ordinal));
        HasRepair(DiagnosticCodes.UnknownEvent).ShouldBeFalse();
    }
}
