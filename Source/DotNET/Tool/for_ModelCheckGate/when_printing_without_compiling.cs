// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.for_ModelCheckGate;

public class when_printing_without_compiling : for_ModelCheck.given.a_model
{
    int _exitCode;
    void Establish() => File.WriteAllText(Path.Combine(Root, "screenplay.json"), "{\"validation\":{\"checks\":[\"PLAY0534\",\"navigation\"],\"warningsAsErrors\":true,\"scope\":\"NotCompiled\",\"openQuestions\":\"fail\"}}");
    void Because() => _exitCode = ModelCheck.Run([Root, "--print-gate", "--format", "json"], Output, Error);
    [Fact] void should_not_compile_the_broken_source_or_validate_the_scope() => _exitCode.ShouldEqual(0);
    [Fact] void should_print_the_expanded_check_names() => Output.ToString().ShouldContain("Checks: field-origins, navigation (source: file)");
    [Fact] void should_show_declared_warnings_as_errors() => Output.ToString().ShouldContain("Warnings as errors: true (source: file)");
    [Fact] void should_show_the_root_and_path() => Output.ToString().ShouldContain($"Gate file: {Path.Combine(Root, "screenplay.json")}");
    [Fact] void should_not_emit_diagnostics() => Output.ToString().ShouldNotContain("PLAY0028");
    [Fact] void should_not_write_errors() => Error.ToString().ShouldEqual(string.Empty);
}
