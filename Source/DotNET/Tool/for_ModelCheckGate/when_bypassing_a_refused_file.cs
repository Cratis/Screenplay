// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.for_ModelCheckGate;

public class when_bypassing_a_refused_file : for_ModelCheck.given.a_model
{
    int _exitCode;
    void Establish() => Directory.CreateDirectory(Path.Combine(Root, "screenplay.json"));
    void Because() => _exitCode = ModelCheck.Run([Root, "--no-gate-file", "--print-gate", "--check", "navigation"], Output, Error);
    [Fact] void should_ignore_the_refused_path() => _exitCode.ShouldEqual(0);
    [Fact] void should_preserve_explicit_checks() => Output.ToString().ShouldContain("Checks: navigation (source: commandLine)");
    [Fact] void should_use_default_warning_behavior() => Output.ToString().ShouldContain("Warnings as errors: false (source: default)");
}
