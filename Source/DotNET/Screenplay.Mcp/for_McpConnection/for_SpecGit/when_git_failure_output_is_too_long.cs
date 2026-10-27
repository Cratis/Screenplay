// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpConnection.for_SpecGit;

public class when_git_failure_output_is_too_long : Specification
{
    Exception _failure;
    string _error;

    void Because()
    {
        _failure = Catch.Exception(() => given.SpecGit.Run(Path.GetTempPath(), [new string('x', 5000)]));
        _error = _failure.Message.Split("; stderr: ")[1];
    }

    [Fact] void should_fail() => _failure.ShouldBeOfExactType<McpFailure>();
    [Fact] void should_mark_the_truncation() => _error.StartsWith("[truncated] ", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_keep_only_the_output_tail() => _error.Length.ShouldEqual(4000 + "[truncated] ".Length);
    [Fact] void should_preserve_the_end_of_the_diagnostic() => _error.EndsWith("See 'git --help'.", StringComparison.Ordinal).ShouldBeTrue();
}
