// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.for_ModelCheckGate;

public class when_gating_declared_completeness : for_ModelCheck.given.a_model
{
    int _exitCode;

    void Establish()
    {
        File.WriteAllText(Path.Combine(Root, "screenplay.json"), "{\"validation\":{\"checks\":[\"data-bindings\",\"PLAY0531\"],\"scope\":\"M.F.View\",\"warningsAsErrors\":true}}");
        File.WriteAllText(Path.Combine(Root, "application.play"), """
            module M
              feature F
                slice StateView View
                  readmodel R
                    value String
                  query Q => R[]
                  screen S
                    data R via query Q
            """);
    }

    void Because() => _exitCode = ModelCheck.Run([Root], Output, Error);
    [Fact] void should_run_the_declared_checks_without_flags() => _exitCode.ShouldEqual(1);
    [Fact] void should_count_aliases_only_once() => Output.ToString().ShouldContain("0 error(s), 1 warning(s) in scope");
    [Fact] void should_use_the_declared_scope() => Output.ToString().ShouldContain("Scope M.F.View:");
}
