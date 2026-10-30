// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpConnection.for_SpecGit;

public class when_git_fails : Specification
{
    Exception _failure;

    void Because() => _failure = Catch.Exception(() => given.SpecGit.Run(Path.GetTempPath(), ["not-a-git-command"]));

    [Fact] void should_fail() => _failure.ShouldBeOfExactType<McpFailure>();
    [Fact] void should_name_the_git_command() => _failure.Message.ShouldContain("git not-a-git-command (in ");
    [Fact] void should_report_what_git_said() => _failure.Message.ShouldContain("stderr: ");
}
