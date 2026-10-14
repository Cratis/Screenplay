// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.for_ModelCheckGate;

public class when_keeping_legacy_output : for_ModelCheck.given.a_model
{
    int _exitCode;
    void Establish() => File.WriteAllText(Path.Combine(Root, "application.play"), "module M\n");
    void Because() => _exitCode = ModelCheck.Run([Root], Output, Error);
    [Fact] void should_remain_clean() => _exitCode.ShouldEqual(0);
    [Fact] void should_preserve_the_exact_legacy_summary() => Output.ToString().ShouldEqual($"1 file(s) compiled - 0 error(s), 0 warning(s){Environment.NewLine}");
    [Fact] void should_preserve_empty_stderr() => Error.ToString().ShouldEqual(string.Empty);
}
