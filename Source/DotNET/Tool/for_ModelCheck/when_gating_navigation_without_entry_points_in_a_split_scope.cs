// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.for_ModelCheck;

public class when_gating_navigation_without_entry_points_in_a_split_scope : given.a_model
{
    int _exitCode;

    void Establish()
    {
        File.WriteAllText(Path.Combine(Root, "application.play"), "module Other\n  description \"First document\"");
        File.WriteAllText(Path.Combine(Root, "b.play"), """
            module M
              feature F
                slice StateView View
                  screen Home
                    title "Home"
            """);
    }

    void Because() => _exitCode = ModelCheck.Run([Root, "--scope", "M.F.View", "--check", "navigation", "--warnaserror"], Output, Error);

    [Fact] void should_gate_the_application_wide_warning() => _exitCode.ShouldEqual(1);
    [Fact] void should_include_it_in_the_selected_scope() => Output.ToString().ShouldContain("0 error(s), 1 warning(s) in scope");
    [Fact] void should_explain_the_missing_entry_points() => Output.ToString().ShouldContain("no navigation entry points");
}
