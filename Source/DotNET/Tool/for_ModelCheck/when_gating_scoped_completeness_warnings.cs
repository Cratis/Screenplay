// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.for_ModelCheck;

public class when_gating_scoped_completeness_warnings : given.a_model
{
    int _exitCode;

    void Establish() => File.WriteAllText(Path.Combine(Root, "application.play"), """
        module M
          feature F
            slice StateView View
              readmodel R
                value String
              query Q => R[]
              screen S
                data R via query Q
        """);

    void Because() => _exitCode = ModelCheck.Run([Root, "--scope", "M.F.View", "--check", "data-bindings", "--check", "PLAY0531", "--warnaserror"], Output, Error);

    [Fact] void should_gate_the_warning() => _exitCode.ShouldEqual(1);
    [Fact] void should_merge_into_the_scoped_summary() => Output.ToString().ShouldContain("0 error(s), 1 warning(s) in scope");
    [Fact] void should_count_repeated_selections_once() => Output.ToString().ShouldContain("Whole application: 0 error(s), 1 warning(s)");
}
