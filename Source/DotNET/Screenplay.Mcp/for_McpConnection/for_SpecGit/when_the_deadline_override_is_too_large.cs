// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Screenplay.Mcp.for_McpConnection.for_SpecGit;

public class when_the_deadline_override_is_too_large : Specification
{
    Exception _failure;
    bool _processStarted;

    void Because() => _failure = Catch.Exception(() => given.SpecGit.Run(
        Path.GetTempPath(), ["controlled-process"], TimeSpan.FromSeconds((int.MaxValue / 1000) + 1), Start, process => process.WaitForExitAsync()));

    Process Start(ProcessStartInfo start)
    {
        _processStarted = true;
        throw new InvalidOperationException("No process should be started.");
    }

    [Fact] void should_fail_clearly() => _failure.ShouldBeOfExactType<McpFailure>();
    [Fact] void should_name_the_override() => _failure.Message.ShouldContain(given.SpecGit.TimeoutVariable);
    [Fact] void should_name_the_maximum() => _failure.Message.ShouldContain($"{int.MaxValue} milliseconds");
    [Fact] void should_not_start_a_process() => _processStarted.ShouldBeFalse();
}
