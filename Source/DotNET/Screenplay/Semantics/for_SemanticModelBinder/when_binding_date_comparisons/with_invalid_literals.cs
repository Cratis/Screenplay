// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_date_comparisons;

public class with_invalid_literals : given.a_date_comparison
{
    readonly List<CompilationResult<SemanticCompilation>> _results = [];

    void Because()
    {
        foreach (var (primitive, literal) in new[] { ("Date", "2026-02-30"), ("DateTime", "2026-01-01T01:00:00.0000000"), ("DateTime", "invalid") })
        {
            foreach (var op in Operators)
            {
                _results.Add(Bind(Model(primitive, $"require to {op} \"{literal}\"")));
                _results.Add(Bind(Model(primitive, $"to {op} \"{literal}\"")));
            }
        }
    }

    [Fact] void should_check_thirty_six_admitted_comparisons() => _results.Count.ShouldEqual(36);
    [Fact] void should_refuse_invalid_dates_and_offsetless_instants_at_binding() => _results.TrueForAll(result => !result.Success && result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidSemanticBinding)).ShouldBeTrue();
    [Fact] void should_not_confuse_invalid_literals_with_unadmitted_comparisons() => _results.TrueForAll(result => result.Diagnostics.All(diagnostic => diagnostic.Code != DiagnosticCodes.UnsupportedSemanticSyntax)).ShouldBeTrue();
}
