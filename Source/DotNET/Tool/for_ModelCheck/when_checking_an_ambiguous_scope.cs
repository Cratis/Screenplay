// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.for_ModelCheck;

public class when_checking_an_ambiguous_scope : given.a_model
{
    int _exitCode;

    void Establish() => File.WriteAllText(Path.Combine(Root, "application.play"), "module M\n  feature F\n    slice StateChange Duplicate\n    slice StateChange Duplicate");
    void Because() => _exitCode = ModelCheck.Run([Root, "--scope", "M.F.Duplicate"], Output, Error);

    [Fact] void should_refuse_the_check() => _exitCode.ShouldEqual(2);
    [Fact] void should_explain_the_ambiguity() => Error.ToString().ShouldContain("Ambiguous scope 'M.F.Duplicate'");
}
