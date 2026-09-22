// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.Mcp.for_McpRoot;

public class when_the_file_limit_is_exceeded : for_McpConnection.given.a_connection
{
    Exception? _error;

    void Establish()
    {
        for (var index = 0; index < McpRoot.MaximumFiles; index++)
        {
            File.WriteAllText(Path.Combine(RootPath, $"file-{index}.play"), string.Empty);
        }
    }

    void Because() => _error = Catch.Exception(() => Root.Read());

    [Fact] void should_fail_instead_of_returning_a_truncated_workspace() => _error.ShouldBeOfExactType<McpFailure>();
}
