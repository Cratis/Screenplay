// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_refusing_invalid_example_fixtures : given.a_connection
{
    Exception? _error;

    void Establish() => File.WriteAllText(Path.Combine(RootPath, "application.play"), """
        example Missing : Unknown
          count = 1
        module Records
          feature Entries
            slice StateChange Record
              specification Recording
                when Missing
        """);

    void Because() => _error = Catch.Exception(() => McpFixtureQueries.Values(new McpSnapshot(Root.Read()), null, null, null, null));

    [Fact] void should_refuse_instead_of_showing_authored_values_as_effective() => _error.ShouldBeOfExactType<McpFailure>();
    [Fact] void should_name_the_unresolved_type() => _error!.Message.ShouldContain("Unknown");
}
