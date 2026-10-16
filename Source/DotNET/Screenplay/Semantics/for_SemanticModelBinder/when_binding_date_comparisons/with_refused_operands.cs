// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_protected_reads.given;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_date_comparisons;

public class with_refused_operands : given.a_date_comparison
{
    readonly List<CompilationResult<SemanticCompilation>> _results = [];

    void Because()
    {
        foreach (var (primitive, op) in new[] { "Date", "DateTime" }.SelectMany(primitive => Operators.Select(op => (primitive, op))))
        {
            _results.Add(Bind(Model(primitive, $"require to {op} from").Replace($"from {primitive}", $"from {(primitive == "Date" ? "DateTime" : "Date")}", StringComparison.Ordinal)));
            _results.Add(Bind(Model(primitive, $"require to {op} today")));
            _results.Add(Bind(Model(primitive, $"require to {op} $context.occurred")));
            _results.Add(Bind(Model(primitive, $"to {op} from")));
            _results.Add(Bind("command reads protected\n" + Model(primitive, $"require $context.occurred {op} to")));
            _results.Add(Bind(Model(primitive, $"require to {op} from").Replace($"from {primitive}", $"from {primitive} optional", StringComparison.Ordinal)));
            _results.Add(Bind(Model(primitive, $"require to {op} \"not-a-date\"").Replace($"to {primitive}", $"to {primitive}[]", StringComparison.Ordinal)));
            _results.Add(Bind(Model(primitive, $"require to {op} \"not-a-date\"").Replace($"to {primitive}", $"to {primitive} optional", StringComparison.Ordinal)));
            _results.Add(Bind($"concept Day : {primitive}\n  validate\n    {op} \"not-a-date\"\n" + Model("String", "id not empty")));
        }

        var readModel = a_protected_read_model.Source.Replace("value String", "value DateTime", StringComparison.Ordinal);
        _results.Add(Bind(readModel.Replace("require View.value != \"\"", "require View.value >= \"not-a-date\"", StringComparison.Ordinal)));
        _results.Add(Bind(readModel.Replace("require View.value != \"\"", "require value >= View.value", StringComparison.Ordinal)));
        _results.Add(Bind("""
            module M
              feature F
                slice Automation React
                  event Started
                    to DateTime
                  event Finished
                  reaction Finish
                    when Started
                      to
                      produces Finished
                    where to >= "not-a-date"
            """));
    }

    [Fact] void should_examine_every_refused_position() => _results.Count.ShouldEqual(111);
    [Fact] void should_refuse_them_all() => _results.TrueForAll(result => !result.Success).ShouldBeTrue();
    [Fact] void should_report_only_the_admission_diagnostic_even_for_invalid_literals() => Assert.True(
        _results.TrueForAll(result => result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.UnsupportedSemanticSyntax) && result.Diagnostics.All(diagnostic => diagnostic.Code != DiagnosticCodes.InvalidSemanticBinding)),
        string.Join('\n', _results.SelectMany(result => result.Diagnostics).Select(diagnostic => diagnostic.ToString())));
}
