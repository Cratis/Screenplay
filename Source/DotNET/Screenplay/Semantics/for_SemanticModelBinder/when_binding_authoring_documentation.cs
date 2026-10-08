// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_authoring_documentation : given.a_semantic_binder
{
    const string Source = """
        module M
          feature F
            slice StateChange S
              command C
                id Uuid identifier
                produces event E
                  value String = "issued"
              specification Case
                when C
                  id = "11111111-1111-1111-1111-111111111111"
                then E
                  value = "issued"
            slice StateView View
              readmodel V
                id Uuid
                value String
              query VById => V optional
                by id Uuid
              projection P => V
                from E
                  value = value
            slice Automation Work
              reaction R
                every 15 minutes
        """;

    CompilationResult<SemanticCompilation> _plain;
    CompilationResult<SemanticCompilation> _documented;

    void Because()
    {
        _plain = Bind(Source);
        var source = Source;
        foreach (var header in new[] { "module M", "  feature F", "    slice StateChange S", "      command C", "      readmodel V", "      reaction R" })
        {
            var indent = new string(' ', header.TakeWhile(character => character == ' ').Count() + 2);
            source = source.Replace(header + "\n", $"{header}\n{indent}documentation\n{indent}  ```markdown\n{indent}  Why this choice.\n{indent}  ```\n", StringComparison.Ordinal);
        }

        source = source.Replace("      specification Case\n", "      specification Case\n        description \"The rule witnessed\"\n", StringComparison.Ordinal);
        _documented = Bind(source);
    }

    [Fact] void should_bind_the_baseline() => _plain.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).Select(diagnostic => diagnostic.Message).ShouldBeEmpty();
    [Fact] void should_bind_the_documented_model() => _documented.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).Select(diagnostic => diagnostic.Message).ShouldBeEmpty();
    [Fact] void should_report_every_added_metadata_field() => _documented.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.ReportOnlySemanticSyntax).ShouldEqual(7);
    [Fact] void should_keep_executable_bytes_unchanged() => SemanticModelSerializer.Serialize(_plain.Value!.Model).SequenceEqual(SemanticModelSerializer.Serialize(_documented.Value!.Model)).ShouldBeTrue();
}
