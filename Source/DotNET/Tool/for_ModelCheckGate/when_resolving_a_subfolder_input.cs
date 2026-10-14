// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.for_ModelCheckGate;

public class when_resolving_a_subfolder_input : for_ModelCheck.given.a_model
{
    int _exitCode;
    string _target;

    void Establish()
    {
        File.WriteAllText(Path.Combine(Root, "screenplay.json"), "{\"validation\":{\"warningsAsErrors\":true}}");
        _target = Path.Combine(Root, "child");
        Directory.CreateDirectory(_target);
    }

    void Because() => _exitCode = ModelCheck.Run([_target, "--print-gate"], Output, Error);
    [Fact] void should_resolve_the_parent_gate() => Output.ToString().ShouldContain($"Model root: {Root}");
    [Fact] void should_use_the_parent_settings() => Output.ToString().ShouldContain("Warnings as errors: true (source: file)");
    [Fact] void should_not_compile_sources_outside_the_target() => _exitCode.ShouldEqual(0);
}
