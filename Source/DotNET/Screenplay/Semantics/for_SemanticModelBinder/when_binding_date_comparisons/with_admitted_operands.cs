// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_date_comparisons;

public class with_admitted_operands : given.a_date_comparison
{
    readonly List<CompilationResult<SemanticCompilation>> _results = [];

    void Because()
    {
        foreach (var (primitive, op) in new[] { "Date", "DateTime" }.SelectMany(primitive => Operators.Select(op => (primitive, op))))
        {
            var literal = primitive == "Date" ? "\"2026-01-01\"" : "\"2026-01-01T01:00:00+01:00\"";
            _results.Add(Bind(Model(primitive, $"require to {op} from")));
            _results.Add(Bind(Model(primitive, $"require to {op} {literal}")));
            _results.Add(Bind(Model(primitive, $"to {op} {literal}")));
            _results.Add(Bind(Model(primitive, "id not empty").Replace("produces WindowKept\n          for id", $"produces when to {op} from\n          WindowKept\n            for id", StringComparison.Ordinal)));
            _results.Add(Bind(Model(primitive, "id not empty").Replace("produces WindowKept\n          for id", $"produces when to {op} {literal}\n          WindowKept\n            for id", StringComparison.Ordinal)));
        }
    }

    [Fact] void should_cover_both_primitives_all_six_operators_and_each_operand_position() => _results.Count.ShouldEqual(60);
    [Fact] void should_bind_every_comparison() => Assert.True(_results.TrueForAll(result => result.Success), string.Join('\n', _results.SelectMany(result => result.Diagnostics).Select(diagnostic => diagnostic.Message)));
    [Fact] void should_select_v26_only_for_these_models() => _results.Where(result => result.Success).All(result => result.Value!.Model.SemanticVersion == SemanticVersion.V26).ShouldBeTrue();
    [Fact] void should_canonicalize_the_offset_literal() => Command(_results[32].Value!.Model).Validations.Single().Operand.ShouldEqual(SemanticValue.Text("2026-01-01T00:00:00.0000000Z"));
}
