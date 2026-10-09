// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.for_ModelTest;

public class when_the_model_is_unbound : given.a_model
{
    int _exit;

    void Establish()
    {
        var path = Path.Combine(Root, "application.play");
        File.WriteAllText(path, File.ReadAllText(path).Replace("      command Register\n", "      command Register\n        concurrency\n          eventSource\n", StringComparison.Ordinal));
    }
    void Because() => _exit = ModelTest.Run([Root], Output, Error);

    [Fact] void should_have_a_distinct_nonzero_exit() => _exit.ShouldEqual(3);
    [Fact] void should_print_the_executable_diagnostics() => Output.ToString().ShouldContain("PLAY0271");
    [Fact] void should_not_execute_the_scenarios() => Output.ToString().ShouldContain("0 executed");
}
