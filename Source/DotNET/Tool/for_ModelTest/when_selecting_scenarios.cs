// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.for_ModelTest;

public class when_selecting_scenarios : given.a_model
{
    int _exit;

    void Because() => _exit = ModelTest.Run([Path.Combine(Root, "application.play"), "--filter", "M.F.Register.Correct"], Output, Error);

    [Fact] void should_pass_only_the_selected_scenario() => _exit.ShouldEqual(0);
    [Fact] void should_print_a_text_summary() => Output.ToString().ShouldContain("2 discovered, 1 selected, 1 executed; 1 passed");
}
