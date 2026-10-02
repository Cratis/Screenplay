// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.for_Documentation.given;

namespace Cratis.Screenplay.for_Documentation;

public class when_compiling_the_command_examples : Specification
{
    DocumentationExample[] _examples;
    string[] _errors;

    void Establish() => _examples = [.. DocumentationExamples.All().Where(example => example.Path.Replace('\\', '/') == "screenplay/commands.md")];

    void Because() => _errors = [.. _examples.SelectMany(example => new ScreenplayCompiler().Compile(example.Source).Diagnostics
        .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
        .Select(diagnostic => $"{example.Reference}: {diagnostic.Code} {diagnostic.Message}"))];

    [Fact] void should_check_the_concrete_command_examples() => _examples.Length.ShouldBeGreaterThan(20);
    [Fact] void should_check_command_body_fragments() => _examples.Any(example => example.Kind == "command body").ShouldBeTrue();
    [Fact] void should_compile_every_example() => string.Join('\n', _errors).ShouldEqual(string.Empty);
}
