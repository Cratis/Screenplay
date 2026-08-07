// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.for_Documentation.given;

namespace Cratis.Screenplay.for_Documentation;

public class when_compiling_every_example : Specification
{
    ScreenplayCompiler _compiler;
    List<(DocumentationExample Example, IEnumerable<Diagnostic> Errors)> _broken;
    int _compiled;

    void Establish() => _compiler = new();

    void Because()
    {
        _broken = [];
        foreach (var example in DocumentationExamples.All())
        {
            _compiled++;
            var diagnostics = example.Language == "pdl"
                ? _compiler.CompileProjection(example.Source).Diagnostics
                : _compiler.Compile(example.Source).Diagnostics;

            var errors = diagnostics.Where(_ => _.Severity == DiagnosticSeverity.Error).ToList();
            if (errors.Count > 0)
            {
                _broken.Add((example, errors));
            }
        }
    }

    [Fact] void should_find_examples_to_compile() => _compiled.ShouldBeGreaterThan(200);
    [Fact] void should_compile_every_one_of_them() => Report().ShouldEqual(string.Empty);

    string Report()
    {
        if (_broken.Count == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder($"{_broken.Count} of {_compiled} documentation examples do not compile:").Append('\n');
        foreach (var (example, errors) in _broken)
        {
            builder.Append(example.Reference).Append('\n');
            foreach (var error in errors)
            {
                builder.Append("    ").Append(error.Code).Append("  ").Append(error.Message).Append('\n');
            }
        }

        return builder.ToString();
    }
}
